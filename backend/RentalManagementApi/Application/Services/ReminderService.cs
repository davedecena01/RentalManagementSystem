using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RentalManagementApi.Data;
using RentalManagementApi.DTOs.Reminders;
using RentalManagementApi.Entities;
using RentalManagementApi.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace RentalManagementApi.Application.Services;

public class ReminderService(AppDbContext db, IOptions<SendGridOptions> sendGridOptions, ILogger<ReminderService> logger)
{
    public async Task<ReminderSettingDto> GetOrCreateAsync(Guid leaseId)
    {
        var setting = await db.ReminderSettings.FirstOrDefaultAsync(r => r.LeaseId == leaseId);
        if (setting is null)
        {
            setting = new ReminderSetting { LeaseId = leaseId };
            db.ReminderSettings.Add(setting);
            await db.SaveChangesAsync();
        }
        return ToDto(setting);
    }

    public async Task<ReminderSettingDto?> UpdateAsync(Guid leaseId, Guid landlordId, UpdateReminderSettingRequest request)
    {
        var lease = await db.Leases
            .Include(l => l.Property)
            .FirstOrDefaultAsync(l => l.Id == leaseId && l.Property.LandlordId == landlordId);
        if (lease is null) return null;

        var setting = await db.ReminderSettings.FirstOrDefaultAsync(r => r.LeaseId == leaseId);
        if (setting is null)
        {
            setting = new ReminderSetting { LeaseId = leaseId };
            db.ReminderSettings.Add(setting);
        }

        setting.IsEnabled = request.IsEnabled;
        setting.DaysBeforeDue = Math.Max(0, request.DaysBeforeDue);
        setting.DaysAfterDue = Math.Max(0, request.DaysAfterDue);
        setting.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ToDto(setting);
    }

    public async Task<TriggerRemindersResult> TriggerAsync()
    {
        var result = new TriggerRemindersResult();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var settings = await db.ReminderSettings
            .Where(r => r.IsEnabled)
            .Include(r => r.Lease)
                .ThenInclude(l => l.Tenant)
            .Include(r => r.Lease)
                .ThenInclude(l => l.Property)
            .Where(r => r.Lease.Status == LeaseStatus.Active)
            .ToListAsync();

        foreach (var setting in settings)
        {
            var payments = await db.Payments
                .Where(p => p.LeaseId == setting.LeaseId &&
                            (p.Status == PaymentStatus.Unpaid || p.Status == PaymentStatus.Partial))
                .ToListAsync();

            foreach (var payment in payments)
            {
                var daysUntilDue = payment.DueDate.DayNumber - today.DayNumber;

                string? reminderType = null;
                if (daysUntilDue == setting.DaysBeforeDue && setting.DaysBeforeDue > 0)
                    reminderType = "before";
                else if (daysUntilDue == -setting.DaysAfterDue && setting.DaysAfterDue > 0)
                    reminderType = "after";

                if (reminderType is null) continue;

                var alreadySent = await db.ReminderLogs.AnyAsync(l =>
                    l.LeaseId == setting.LeaseId &&
                    l.DueDate == payment.DueDate &&
                    l.ReminderType == reminderType &&
                    l.SentDate == today);

                if (alreadySent) { result.Skipped++; continue; }

                var (success, error) = await SendReminderEmailAsync(setting, payment, reminderType, daysUntilDue);

                db.ReminderLogs.Add(new ReminderLog
                {
                    LeaseId = setting.LeaseId,
                    DueDate = payment.DueDate,
                    ReminderType = reminderType,
                    SentDate = today,
                    Success = success,
                    ErrorMessage = error
                });

                if (success) result.Sent++;
                else result.Errors.Add($"Lease {setting.LeaseId} / {payment.DueDate}: {error}");
            }
        }

        await db.SaveChangesAsync();
        return result;
    }

    private async Task<(bool Success, string? Error)> SendReminderEmailAsync(
        ReminderSetting setting, Payment payment, string reminderType, int daysUntilDue)
    {
        var opts = sendGridOptions.Value;
        if (string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            logger.LogWarning("SendGrid not configured — skipping email for lease {LeaseId}", setting.LeaseId);
            return (false, "SendGrid not configured");
        }

        var tenant = setting.Lease.Tenant;
        var property = setting.Lease.Property;
        var dueMonth = payment.DueDate.ToString("MMMM yyyy");
        var balance = payment.AmountDue - payment.AmountPaid;

        string subject, body;
        if (reminderType == "before")
        {
            subject = $"Rent Due in {daysUntilDue} Day{(daysUntilDue == 1 ? "" : "s")} — {property.Name}";
            body = $"Hi {tenant.FirstName},\n\nThis is a reminder that your rent for {property.Name} ({dueMonth}) is due in {daysUntilDue} day{(daysUntilDue == 1 ? "" : "s")}.\n\nBalance due: ₱{balance:N2}\n\nPlease log in to the tenant portal to make your payment.\n\nThank you,\nRental Property Manager";
        }
        else
        {
            subject = $"Rent Overdue — {property.Name}";
            body = $"Hi {tenant.FirstName},\n\nYour rent for {property.Name} ({dueMonth}) is overdue.\n\nBalance due: ₱{balance:N2}\n\nPlease log in to the tenant portal to make your payment as soon as possible.\n\nThank you,\nRental Property Manager";
        }

        try
        {
            var client = new SendGridClient(opts.ApiKey);
            var from = new EmailAddress(opts.FromEmail, opts.FromName);
            var to = new EmailAddress(tenant.Email, $"{tenant.FirstName} {tenant.LastName}");
            var msg = MailHelper.CreateSingleEmail(from, to, subject, body, body.Replace("\n", "<br>"));
            var response = await client.SendEmailAsync(msg);

            if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
                return (true, null);

            var err = await response.Body.ReadAsStringAsync();
            logger.LogError("SendGrid error {Status}: {Body}", response.StatusCode, err);
            return (false, $"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SendGrid exception for lease {LeaseId}", setting.LeaseId);
            return (false, ex.Message);
        }
    }

    private static ReminderSettingDto ToDto(ReminderSetting r) => new()
    {
        Id = r.Id,
        LeaseId = r.LeaseId,
        IsEnabled = r.IsEnabled,
        DaysBeforeDue = r.DaysBeforeDue,
        DaysAfterDue = r.DaysAfterDue
    };
}
