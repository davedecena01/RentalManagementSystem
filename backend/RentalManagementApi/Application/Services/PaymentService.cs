using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RentalManagementApi.Data;
using RentalManagementApi.DTOs.Payments;
using RentalManagementApi.Entities;
using RentalManagementApi.Options;
using Stripe;
using Stripe.Checkout;

namespace RentalManagementApi.Application.Services;

public class PaymentService(AppDbContext db, IOptions<StripeOptions> stripeOptions, AppLogService logService)
{
    public async Task<List<PaymentDto>> GetForLandlordAsync(Guid landlordId, string? statusFilter)
    {
        var query = db.Payments
            .Include(p => p.Lease).ThenInclude(l => l.Property)
            .Include(p => p.Lease).ThenInclude(l => l.Tenant)
            .Where(p => p.Lease.Property.LandlordId == landlordId);

        if (statusFilter is not null && Enum.TryParse<PaymentStatus>(statusFilter, true, out var status))
            query = query.Where(p => p.Status == status);

        return await query.OrderBy(p => p.DueDate).Select(p => ToDto(p)).ToListAsync();
    }

    public async Task<List<PaymentDto>> GetForTenantAsync(Guid tenantId)
    {
        return await db.Payments
            .Include(p => p.Lease).ThenInclude(l => l.Property)
            .Include(p => p.Lease).ThenInclude(l => l.Tenant)
            .Where(p => p.Lease.TenantId == tenantId)
            .OrderBy(p => p.DueDate)
            .Select(p => ToDto(p))
            .ToListAsync();
    }

    public async Task<PaymentDto?> GetByIdAsync(Guid id, Guid userId, string role)
    {
        var payment = await db.Payments
            .Include(p => p.Lease).ThenInclude(l => l.Property)
            .Include(p => p.Lease).ThenInclude(l => l.Tenant)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment is null) return null;

        if (role == "Landlord" && payment.Lease.Property.LandlordId != userId) return null;
        if (role == "Tenant" && payment.Lease.TenantId != userId) return null;

        return ToDto(payment);
    }

    public async Task<(string? Url, string? Error)> CreateStripeCheckoutAsync(Guid leaseId, Guid tenantId, string frontendUrl)
    {
        var lease = await db.Leases
            .Include(l => l.Property)
            .FirstOrDefaultAsync(l => l.Id == leaseId && l.TenantId == tenantId);
        if (lease is null) return (null, "LEASE_NOT_FOUND");

        var unpaidPayment = await db.Payments
            .Where(p => p.LeaseId == leaseId && p.Status != PaymentStatus.Paid)
            .OrderBy(p => p.DueDate)
            .FirstOrDefaultAsync();
        if (unpaidPayment is null) return (null, "NO_UNPAID_PAYMENT");

        StripeConfiguration.ApiKey = stripeOptions.Value.SecretKey;

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "php",
                        UnitAmount = (long)(unpaidPayment.AmountDue * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Rent — {lease.Property.Name} ({unpaidPayment.DueDate:MMM yyyy})"
                        }
                    },
                    Quantity = 1
                }
            ],
            Mode = "payment",
            SuccessUrl = $"{frontendUrl}/payments?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{frontendUrl}/payments",
            Metadata = new Dictionary<string, string>
            {
                ["payment_id"] = unpaidPayment.Id.ToString()
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        unpaidPayment.StripeSessionId = session.Id;
        await db.SaveChangesAsync();

        return (session.Url, null);
    }

    public async Task HandleStripeWebhookAsync(string payload, string signature, string webhookSecret)
    {
        var stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret);

        if (stripeEvent.Type != "checkout.session.completed") return;

        var session = stripeEvent.Data.Object as Session;
        if (session is null) return;

        if (!session.Metadata.TryGetValue("payment_id", out var paymentIdStr)) return;
        if (!Guid.TryParse(paymentIdStr, out var paymentId)) return;

        var payment = await db.Payments
            .Include(p => p.Lease).ThenInclude(l => l.Property)
            .FirstOrDefaultAsync(p => p.Id == paymentId);
        if (payment is null || payment.Status == PaymentStatus.Paid) return;

        payment.AmountPaid = payment.AmountDue;
        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;
        logService.Log(payment.Lease.Property.LandlordId, "payment.stripe_paid", "Payment", payment.Id,
            $"Stripe payment confirmed for {payment.Lease.Property.Name} — ₱{payment.AmountDue:N2}");
        await db.SaveChangesAsync();
    }

    public async Task<(PaymentDto? Payment, string? Error)> ManualPayAsync(Guid paymentId, Guid userId, string role, ManualPayRequest request)
    {
        var payment = await db.Payments
            .Include(p => p.Lease).ThenInclude(l => l.Property)
            .Include(p => p.Lease).ThenInclude(l => l.Tenant)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment is null) return (null, "NOT_FOUND");

        if (role == "Tenant" && payment.Lease.TenantId != userId) return (null, "FORBIDDEN");
        if (role == "Landlord" && payment.Lease.Property.LandlordId != userId) return (null, "FORBIDDEN");

        if (request.AmountPaid <= 0) return (null, "INVALID_AMOUNT");

        payment.AmountPaid += request.AmountPaid;
        if (request.ProofFileUrl is not null) payment.ProofFileUrl = request.ProofFileUrl;
        if (request.Notes is not null) payment.Notes = request.Notes;

        payment.Status = payment.AmountPaid >= payment.AmountDue
            ? PaymentStatus.Paid
            : PaymentStatus.Partial;

        if (payment.Status == PaymentStatus.Paid)
            payment.PaidAt = DateTime.UtcNow;

        payment.UpdatedAt = DateTime.UtcNow;
        var landlordId = payment.Lease.Property.LandlordId;
        logService.Log(landlordId, "payment.recorded", "Payment", payment.Id,
            $"Manual payment of ₱{request.AmountPaid:N2} recorded for {payment.Lease.Property.Name}");
        await db.SaveChangesAsync();

        return (ToDto(payment), null);
    }

    private static PaymentDto ToDto(Payment p) => new()
    {
        Id = p.Id,
        LeaseId = p.LeaseId,
        PropertyName = p.Lease.Property.Name,
        TenantName = $"{p.Lease.Tenant.FirstName} {p.Lease.Tenant.LastName}",
        DueDate = p.DueDate,
        AmountDue = p.AmountDue,
        AmountPaid = p.AmountPaid,
        Status = p.Status.ToString(),
        PaidAt = p.PaidAt,
        ProofFileUrl = p.ProofFileUrl,
        Notes = p.Notes,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
