using Microsoft.EntityFrameworkCore;
using RentalManagementApi.Data;
using RentalManagementApi.Entities;

namespace RentalManagementApi.Application.Services;

public class SeedService(AppDbContext db)
{
    public async Task<(bool AlreadySeeded, object? Summary)> SeedDemoAsync(Guid landlordId)
    {
        var exists = await db.Properties
            .AnyAsync(p => p.LandlordId == landlordId && p.Name == "Sunset Apartments Unit 1");
        if (exists) return (true, null);

        var now = DateTime.UtcNow;

        // Demo tenants — created as DB-only users (no Supabase Auth)
        var tenant1 = await db.Users.FirstOrDefaultAsync(u => u.Email == "tenant1@demo.com");
        if (tenant1 is null)
        {
            tenant1 = new User
            {
                Id = Guid.NewGuid(),
                Email = "tenant1@demo.com",
                FirstName = "Alex",
                LastName = "Demo",
                Role = "Tenant",
                OnboardingCompleted = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Users.Add(tenant1);
        }

        var tenant2 = await db.Users.FirstOrDefaultAsync(u => u.Email == "tenant2@demo.com");
        if (tenant2 is null)
        {
            tenant2 = new User
            {
                Id = Guid.NewGuid(),
                Email = "tenant2@demo.com",
                FirstName = "Jordan",
                LastName = "Demo",
                Role = "Tenant",
                OnboardingCompleted = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Users.Add(tenant2);
        }

        await db.SaveChangesAsync();

        // Properties
        var prop1 = new Property
        {
            LandlordId = landlordId,
            Name = "Sunset Apartments Unit 1",
            Address = "123 Sunset Blvd, Unit 1, Makati City",
            Type = PropertyType.Apartment,
            Description = "Furnished 2-bedroom apartment with city view.",
            CreatedAt = now.AddDays(-14),
            UpdatedAt = now.AddDays(-14)
        };
        var prop2 = new Property
        {
            LandlordId = landlordId,
            Name = "Greenview Townhouse",
            Address = "456 Greenview St, BGC, Taguig City",
            Type = PropertyType.House,
            Description = "3-bedroom townhouse with garden.",
            CreatedAt = now.AddDays(-13),
            UpdatedAt = now.AddDays(-13)
        };
        db.Properties.AddRange(prop1, prop2);
        await db.SaveChangesAsync();

        // Leases (manually created — do not use LeaseService to avoid auto-payment generation)
        var leaseStart = DateOnly.FromDateTime(now.AddMonths(-6));
        var leaseEnd = DateOnly.FromDateTime(now.AddMonths(6));

        var lease1 = new Lease
        {
            PropertyId = prop1.Id,
            TenantId = tenant1.Id,
            StartDate = leaseStart,
            EndDate = leaseEnd,
            MonthlyRent = 15000,
            DepositAmount = 30000,
            AdvanceAmount = 15000,
            Status = LeaseStatus.Active,
            CreatedAt = now.AddDays(-12),
            UpdatedAt = now.AddDays(-12)
        };
        var lease2 = new Lease
        {
            PropertyId = prop2.Id,
            TenantId = tenant2.Id,
            StartDate = leaseStart,
            EndDate = leaseEnd,
            MonthlyRent = 22000,
            DepositAmount = 44000,
            AdvanceAmount = 22000,
            Status = LeaseStatus.Active,
            CreatedAt = now.AddDays(-11),
            UpdatedAt = now.AddDays(-11)
        };
        db.Leases.AddRange(lease1, lease2);
        await db.SaveChangesAsync();

        // Payments — 6 specific records (2 Paid, 2 Partial, 2 Unpaid)
        db.Payments.AddRange(
            new Payment
            {
                LeaseId = lease1.Id,
                DueDate = DateOnly.FromDateTime(now.AddMonths(-2)),
                AmountDue = 15000,
                AmountPaid = 15000,
                Status = PaymentStatus.Paid,
                PaidAt = now.AddMonths(-2).AddDays(2)
            },
            new Payment
            {
                LeaseId = lease1.Id,
                DueDate = DateOnly.FromDateTime(now.AddMonths(-1)),
                AmountDue = 15000,
                AmountPaid = 8000,
                Status = PaymentStatus.Partial
            },
            new Payment
            {
                LeaseId = lease1.Id,
                DueDate = DateOnly.FromDateTime(now),
                AmountDue = 15000,
                AmountPaid = 0,
                Status = PaymentStatus.Unpaid
            },
            new Payment
            {
                LeaseId = lease2.Id,
                DueDate = DateOnly.FromDateTime(now.AddMonths(-2)),
                AmountDue = 22000,
                AmountPaid = 22000,
                Status = PaymentStatus.Paid,
                PaidAt = now.AddMonths(-2).AddDays(1)
            },
            new Payment
            {
                LeaseId = lease2.Id,
                DueDate = DateOnly.FromDateTime(now.AddMonths(-1)),
                AmountDue = 22000,
                AmountPaid = 10000,
                Status = PaymentStatus.Partial
            },
            new Payment
            {
                LeaseId = lease2.Id,
                DueDate = DateOnly.FromDateTime(now),
                AmountDue = 22000,
                AmountPaid = 0,
                Status = PaymentStatus.Unpaid
            }
        );

        // Maintenance requests
        var maint1 = new MaintenanceRequest
        {
            PropertyId = prop1.Id,
            TenantId = tenant1.Id,
            Title = "Leaking faucet in kitchen",
            Description = "The kitchen faucet has been dripping constantly for 3 days.",
            Priority = MaintenancePriority.Medium,
            Status = MaintenanceStatus.Open,
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now.AddDays(-1)
        };
        var maint2 = new MaintenanceRequest
        {
            PropertyId = prop1.Id,
            TenantId = tenant1.Id,
            Title = "Broken AC unit",
            Description = "Air conditioning stopped working.",
            Priority = MaintenancePriority.High,
            Status = MaintenanceStatus.InProgress,
            CreatedAt = now.AddDays(-3),
            UpdatedAt = now.AddDays(-2)
        };
        var maint3 = new MaintenanceRequest
        {
            PropertyId = prop2.Id,
            TenantId = tenant2.Id,
            Title = "Clogged bathroom drain",
            Description = "Bathroom drain is draining slowly.",
            Priority = MaintenancePriority.Low,
            Status = MaintenanceStatus.Resolved,
            ResolutionNotes = "Drain cleared and treated.",
            ResolvedAt = now.AddDays(-1),
            CreatedAt = now.AddDays(-7),
            UpdatedAt = now.AddDays(-1)
        };
        db.MaintenanceRequests.AddRange(maint1, maint2, maint3);

        // Activity log entries
        db.AppLogs.AddRange(
            new AppLog { UserId = landlordId, Action = "property.created", EntityType = "Property", EntityId = prop1.Id, Description = $"Created property '{prop1.Name}'", CreatedAt = now.AddDays(-14) },
            new AppLog { UserId = landlordId, Action = "property.created", EntityType = "Property", EntityId = prop2.Id, Description = $"Created property '{prop2.Name}'", CreatedAt = now.AddDays(-13) },
            new AppLog { UserId = landlordId, Action = "lease.created", EntityType = "Lease", EntityId = lease1.Id, Description = $"Lease created for {prop1.Name} — tenant Alex Demo", CreatedAt = now.AddDays(-12) },
            new AppLog { UserId = landlordId, Action = "lease.created", EntityType = "Lease", EntityId = lease2.Id, Description = $"Lease created for {prop2.Name} — tenant Jordan Demo", CreatedAt = now.AddDays(-11) },
            new AppLog { UserId = landlordId, Action = "maintenance.submitted", EntityType = "Maintenance", EntityId = maint1.Id, Description = "Maintenance request submitted: 'Leaking faucet in kitchen'", CreatedAt = now.AddDays(-1) }
        );

        await db.SaveChangesAsync();

        return (false, new { properties = 2, tenants = 2, leases = 2, payments = 6, maintenanceRequests = 3 });
    }
}
