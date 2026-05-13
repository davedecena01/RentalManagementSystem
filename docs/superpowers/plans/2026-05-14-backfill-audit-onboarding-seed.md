# Backfill Wave 3/4: Audit Log, Onboarding Wizard, Demo Seed Data — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement three portfolio-completion features: a structured activity feed surfaced on the dashboard and a dedicated page, a 4-step onboarding wizard for first-time landlords, and a "Load Demo Data" button that seeds realistic portfolio data in one click.

**Architecture:** `AppLog` entity + `AppLogService.Log()` adds entries to the EF context (no save) so the calling service's `SaveChangesAsync()` commits them in the same transaction. The onboarding wizard is a standalone Angular component shown inside the dashboard for landlords where `onboardingCompleted === false`, with step progress persisted in `localStorage`. The seed endpoint creates demo tenants as plain DB users (no Supabase Auth), plus properties, leases, payments, and maintenance records, all scoped to the calling landlord — idempotent by property name.

**Tech Stack:** C# .NET 8, EF Core 9, PostgreSQL, Angular 17+ (signals, `@if/@for` control flow), TypeScript.

---

## File Map

**New backend files:**
- `backend/RentalManagementApi/Entities/AppLog.cs`
- `backend/RentalManagementApi/DTOs/Logs/AppLogDto.cs`
- `backend/RentalManagementApi/Application/Services/AppLogService.cs`
- `backend/RentalManagementApi/Controllers/LogsController.cs`
- `backend/RentalManagementApi/Application/Services/SeedService.cs`
- `backend/RentalManagementApi/Controllers/SeedController.cs`

**Modified backend files:**
- `backend/RentalManagementApi/Data/AppDbContext.cs` — add `AppLogs` DbSet + model config
- `backend/RentalManagementApi/Program.cs` — register `AppLogService`, `SeedService`
- `backend/RentalManagementApi/Application/Services/PropertyService.cs` — inject `AppLogService`, call `Log()` in CreateAsync/UpdateAsync
- `backend/RentalManagementApi/Application/Services/LeaseService.cs` — inject `AppLogService`, call `Log()` in CreateAsync/TerminateAsync
- `backend/RentalManagementApi/Application/Services/MaintenanceService.cs` — inject `AppLogService`, call `Log()` in CreateAsync/ResolveAsync
- `backend/RentalManagementApi/Application/Services/PaymentService.cs` — inject `AppLogService`, call `Log()` in ManualPayAsync/HandleStripeWebhookAsync

**New frontend files:**
- `frontend/rental-management-ui/src/app/core/models/log.model.ts`
- `frontend/rental-management-ui/src/app/features/account/activity/activity.component.ts`
- `frontend/rental-management-ui/src/app/features/account/activity/activity.component.html`
- `frontend/rental-management-ui/src/app/features/dashboard/onboarding-wizard/onboarding-wizard.component.ts`
- `frontend/rental-management-ui/src/app/features/dashboard/onboarding-wizard/onboarding-wizard.component.html`

**Modified frontend files:**
- `frontend/rental-management-ui/src/app/core/services/api.service.ts` — add `getLogs()`, `loadDemoData()`
- `frontend/rental-management-ui/src/app/features/dashboard/dashboard.component.ts` — add activity feed, wizard, seed button
- `frontend/rental-management-ui/src/app/features/dashboard/dashboard.component.html` — render activity card, wizard, seed banner
- `frontend/rental-management-ui/src/app/features/account/account.routes.ts` — add `/account/activity` route

---

## Task 1: Create feature branch + AppLog entity

**Files:**
- Create: `backend/RentalManagementApi/Entities/AppLog.cs`

- [ ] **Step 1: Create the feature branch**

```bash
git checkout main && git pull
git checkout -b feature/backfill-audit-onboarding-seed
```

- [ ] **Step 2: Create `Entities/AppLog.cs`**

```csharp
namespace RentalManagementApi.Entities;

public class AppLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
```

- [ ] **Step 3: Register AppLog in `Data/AppDbContext.cs`**

Add the DbSet after the existing `LeaseProvisions` line:

```csharp
public DbSet<LeaseProvision> LeaseProvisions => Set<LeaseProvision>();
public DbSet<AppLog> AppLogs => Set<AppLog>();
```

Add model config at the end of `OnModelCreating`, before the closing `}`):

```csharp
modelBuilder.Entity<AppLog>(e =>
{
    e.HasKey(a => a.Id);
    e.Property(a => a.Action).IsRequired().HasMaxLength(100);
    e.Property(a => a.EntityType).IsRequired().HasMaxLength(50);
    e.Property(a => a.Description).IsRequired().HasMaxLength(500);
    e.HasIndex(a => new { a.UserId, a.CreatedAt });
    e.HasOne(a => a.User)
     .WithMany()
     .HasForeignKey(a => a.UserId)
     .OnDelete(DeleteBehavior.Cascade);
});
```

- [ ] **Step 4: Create and apply EF Core migration**

```bash
cd backend/RentalManagementApi
dotnet ef migrations add AddAppLogs
dotnet ef database update
```

Expected: migration file created at `Migrations/<timestamp>_AddAppLogs.cs`, database updated with `AppLogs` table.

- [ ] **Step 5: Commit**

```bash
git add backend/RentalManagementApi/Entities/AppLog.cs backend/RentalManagementApi/Data/AppDbContext.cs backend/RentalManagementApi/Migrations/
git commit -m "feat(logs): add AppLog entity and migration"
```

---

## Task 2: AppLogService and DTOs

**Files:**
- Create: `backend/RentalManagementApi/DTOs/Logs/AppLogDto.cs`
- Create: `backend/RentalManagementApi/Application/Services/AppLogService.cs`
- Modify: `backend/RentalManagementApi/Program.cs`

- [ ] **Step 1: Create `DTOs/Logs/AppLogDto.cs`**

```csharp
namespace RentalManagementApi.DTOs.Logs;

public class AppLogDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PagedLogsResult
{
    public List<AppLogDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
```

- [ ] **Step 2: Create `Application/Services/AppLogService.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using RentalManagementApi.Data;
using RentalManagementApi.DTOs.Logs;
using RentalManagementApi.Entities;

namespace RentalManagementApi.Application.Services;

public class AppLogService(AppDbContext db)
{
    /// Adds a log entry to the EF context. Caller must call SaveChangesAsync.
    public void Log(Guid userId, string action, string entityType, Guid? entityId, string description)
    {
        db.AppLogs.Add(new AppLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description
        });
    }

    public async Task<PagedLogsResult> GetLogsAsync(Guid userId, string? entityType, int page, int pageSize)
    {
        var query = db.AppLogs.Where(l => l.UserId == userId);

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(l => l.EntityType == entityType);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AppLogDto
            {
                Id = l.Id,
                Action = l.Action,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                Description = l.Description,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        return new PagedLogsResult { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }
}
```

- [ ] **Step 3: Register `AppLogService` in `Program.cs`**

In `Program.cs`, after `builder.Services.AddScoped<ProvisionService>();` (line 125), add:

```csharp
builder.Services.AddScoped<AppLogService>();
builder.Services.AddScoped<SeedService>();
```

- [ ] **Step 4: Create `Controllers/LogsController.cs`**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Common;

namespace RentalManagementApi.Controllers;

[ApiController]
[Route("api/logs")]
[Authorize(Policy = "LandlordPolicy")]
public class LogsController(AppLogService logService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? entityType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var result = await logService.GetLogsAsync(userId.Value, entityType, page, pageSize);
        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
```

- [ ] **Step 5: Build to verify no compile errors**

```bash
cd backend/RentalManagementApi
dotnet build
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add backend/RentalManagementApi/DTOs/Logs/ backend/RentalManagementApi/Application/Services/AppLogService.cs backend/RentalManagementApi/Controllers/LogsController.cs backend/RentalManagementApi/Program.cs
git commit -m "feat(logs): add AppLogService, DTOs, and LogsController"
```

---

## Task 3: Wire AppLogService into PropertyService

**Files:**
- Modify: `backend/RentalManagementApi/Application/Services/PropertyService.cs`

- [ ] **Step 1: Add `AppLogService` to constructor and log in `CreateAsync`**

Change the constructor from:
```csharp
public class PropertyService(AppDbContext db)
```
to:
```csharp
public class PropertyService(AppDbContext db, AppLogService logService)
```

In `CreateAsync`, add the log call before `await db.SaveChangesAsync()`:

```csharp
// Before: db.Properties.Add(property); await db.SaveChangesAsync();
db.Properties.Add(property);
logService.Log(landlordId, "property.created", "Property", property.Id, $"Created property '{property.Name}'");
await db.SaveChangesAsync();
```

- [ ] **Step 2: Log in `UpdateAsync`**

In `UpdateAsync`, add the log call before `await db.SaveChangesAsync()`:

```csharp
// After: property.UpdatedAt = DateTime.UtcNow;
property.UpdatedAt = DateTime.UtcNow;
logService.Log(landlordId, "property.updated", "Property", property.Id, $"Updated property '{property.Name}'");
await db.SaveChangesAsync();
```

- [ ] **Step 3: Build**

```bash
cd backend/RentalManagementApi && dotnet build
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add backend/RentalManagementApi/Application/Services/PropertyService.cs
git commit -m "feat(logs): wire activity logging into PropertyService"
```

---

## Task 4: Wire AppLogService into LeaseService

**Files:**
- Modify: `backend/RentalManagementApi/Application/Services/LeaseService.cs`

- [ ] **Step 1: Add `AppLogService` to constructor**

Change:
```csharp
public class LeaseService(AppDbContext db)
```
to:
```csharp
public class LeaseService(AppDbContext db, AppLogService logService)
```

- [ ] **Step 2: Log in `CreateAsync` before `await db.SaveChangesAsync()`**

```csharp
// After: db.Leases.Add(lease); ... (the payment generation loop)
// Before: await db.SaveChangesAsync();
logService.Log(landlordId, "lease.created", "Lease", lease.Id,
    $"Lease created for {property.Name} — tenant {tenant.FirstName} {tenant.LastName}");
await db.SaveChangesAsync();
```

- [ ] **Step 3: Log in `TerminateAsync` before `await db.SaveChangesAsync()`**

```csharp
// After: lease.UpdatedAt = DateTime.UtcNow;
// Before: await db.SaveChangesAsync();
logService.Log(landlordId, "lease.terminated", "Lease", lease.Id,
    $"Lease terminated for {lease.Property.Name}");
await db.SaveChangesAsync();
```

- [ ] **Step 4: Build and commit**

```bash
cd backend/RentalManagementApi && dotnet build
git add backend/RentalManagementApi/Application/Services/LeaseService.cs
git commit -m "feat(logs): wire activity logging into LeaseService"
```

---

## Task 5: Wire AppLogService into MaintenanceService

**Files:**
- Modify: `backend/RentalManagementApi/Application/Services/MaintenanceService.cs`

- [ ] **Step 1: Add `AppLogService` to constructor**

Change:
```csharp
public class MaintenanceService(AppDbContext db)
```
to:
```csharp
public class MaintenanceService(AppDbContext db, AppLogService logService)
```

- [ ] **Step 2: Log in `CreateAsync` before `await db.SaveChangesAsync()`**

The `property` variable is already loaded. Use `property.LandlordId` as the userId for the log:

```csharp
// After: db.MaintenanceRequests.Add(request);
// Before: await db.SaveChangesAsync();
logService.Log(property.LandlordId, "maintenance.submitted", "Maintenance", request.Id,
    $"Maintenance request submitted: '{request.Title}'");
await db.SaveChangesAsync();
```

- [ ] **Step 3: Log in `ResolveAsync` before `await db.SaveChangesAsync()`**

```csharp
// After: m.UpdatedAt = DateTime.UtcNow;
// Before: await db.SaveChangesAsync();
logService.Log(landlordId, "maintenance.resolved", "Maintenance", m.Id,
    $"Resolved maintenance request: '{m.Title}'");
await db.SaveChangesAsync();
```

- [ ] **Step 4: Build and commit**

```bash
cd backend/RentalManagementApi && dotnet build
git add backend/RentalManagementApi/Application/Services/MaintenanceService.cs
git commit -m "feat(logs): wire activity logging into MaintenanceService"
```

---

## Task 6: Wire AppLogService into PaymentService

**Files:**
- Modify: `backend/RentalManagementApi/Application/Services/PaymentService.cs`

- [ ] **Step 1: Add `AppLogService` to constructor**

Change:
```csharp
public class PaymentService(AppDbContext db, IOptions<StripeOptions> stripeOptions)
```
to:
```csharp
public class PaymentService(AppDbContext db, IOptions<StripeOptions> stripeOptions, AppLogService logService)
```

- [ ] **Step 2: Log in `ManualPayAsync` before `await db.SaveChangesAsync()`**

The landlord's ID is available via `payment.Lease.Property.LandlordId` (already included):

```csharp
// After: payment.UpdatedAt = DateTime.UtcNow;
// Before: await db.SaveChangesAsync();
var landlordId = payment.Lease.Property.LandlordId;
logService.Log(landlordId, "payment.recorded", "Payment", payment.Id,
    $"Manual payment of ₱{request.AmountPaid:N2} recorded for {payment.Lease.Property.Name}");
await db.SaveChangesAsync();
```

- [ ] **Step 3: Log in `HandleStripeWebhookAsync` — load lease+property for landlord ID**

The existing webhook loads `payment` with `db.Payments.FindAsync(paymentId)` without includes. Replace that single line so we get the landlord:

Replace:
```csharp
var payment = await db.Payments.FindAsync(paymentId);
if (payment is null || payment.Status == PaymentStatus.Paid) return;
```
with:
```csharp
var payment = await db.Payments
    .Include(p => p.Lease).ThenInclude(l => l.Property)
    .FirstOrDefaultAsync(p => p.Id == paymentId);
if (payment is null || payment.Status == PaymentStatus.Paid) return;
```

Then after `payment.UpdatedAt = DateTime.UtcNow;` and before `await db.SaveChangesAsync()`:

```csharp
logService.Log(payment.Lease.Property.LandlordId, "payment.stripe_paid", "Payment", payment.Id,
    $"Stripe payment confirmed for {payment.Lease.Property.Name} — ₱{payment.AmountDue:N2}");
await db.SaveChangesAsync();
```

- [ ] **Step 4: Build and commit**

```bash
cd backend/RentalManagementApi && dotnet build
git add backend/RentalManagementApi/Application/Services/PaymentService.cs
git commit -m "feat(logs): wire activity logging into PaymentService"
```

---

## Task 7: SeedService and SeedController

**Files:**
- Create: `backend/RentalManagementApi/Application/Services/SeedService.cs`
- Create: `backend/RentalManagementApi/Controllers/SeedController.cs`

- [ ] **Step 1: Create `Application/Services/SeedService.cs`**

```csharp
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
```

- [ ] **Step 2: Create `Controllers/SeedController.cs`**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Common;

namespace RentalManagementApi.Controllers;

[ApiController]
[Route("api/seed")]
[Authorize(Policy = "LandlordPolicy")]
public class SeedController(SeedService seedService) : ControllerBase
{
    [HttpPost("demo")]
    public async Task<IActionResult> SeedDemo()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var (alreadySeeded, summary) = await seedService.SeedDemoAsync(userId.Value);

        if (alreadySeeded)
            return Ok(new { message = "Demo data already loaded." });

        return Ok(new { message = "Demo data loaded.", summary });
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
```

- [ ] **Step 3: Build and verify**

```bash
cd backend/RentalManagementApi && dotnet build
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Run existing tests**

```bash
cd backend && dotnet test
```

Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add backend/RentalManagementApi/Application/Services/SeedService.cs backend/RentalManagementApi/Controllers/SeedController.cs
git commit -m "feat(seed): add SeedService and SeedController for demo data"
```

---

## Task 8: Frontend — log model and ApiService methods

**Files:**
- Create: `frontend/rental-management-ui/src/app/core/models/log.model.ts`
- Modify: `frontend/rental-management-ui/src/app/core/services/api.service.ts`

- [ ] **Step 1: Create `core/models/log.model.ts`**

```typescript
export interface AppLog {
  id: string;
  action: string;
  entityType: string;
  entityId?: string;
  description: string;
  createdAt: string;
}

export interface PagedLogsResult {
  items: AppLog[];
  totalCount: number;
  page: number;
  pageSize: number;
}
```

- [ ] **Step 2: Add import and two new methods to `api.service.ts`**

Add the import at the top of the file alongside existing imports:

```typescript
import { AppLog, PagedLogsResult } from '../models/log.model';
```

Add these two methods after the existing `getDashboard()` method:

```typescript
getLogs(entityType?: string, page = 1, pageSize = 20): Observable<PagedLogsResult> {
  let params = new HttpParams()
    .set('page', String(page))
    .set('pageSize', String(pageSize));
  if (entityType) params = params.set('entityType', entityType);
  return this.http.get<PagedLogsResult>(`${this.base}/logs`, { params });
}

loadDemoData(): Observable<{ message: string; summary?: unknown }> {
  return this.http.post<{ message: string; summary?: unknown }>(`${this.base}/seed/demo`, {});
}
```

- [ ] **Step 3: Commit**

```bash
git add frontend/rental-management-ui/src/app/core/models/log.model.ts frontend/rental-management-ui/src/app/core/services/api.service.ts
git commit -m "feat(logs): add log model and ApiService methods for logs and seed"
```

---

## Task 9: Dashboard — Recent Activity card and Demo Seed banner

**Files:**
- Modify: `frontend/rental-management-ui/src/app/features/dashboard/dashboard.component.ts`
- Modify: `frontend/rental-management-ui/src/app/features/dashboard/dashboard.component.html`

- [ ] **Step 1: Update `dashboard.component.ts`**

Replace the full file content with:

```typescript
import { Component, OnInit, AfterViewInit, OnDestroy, ViewChild, ElementRef, inject, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Chart, registerables } from 'chart.js';
import { AuthService } from '../../core/services/auth.service';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { DashboardData } from '../../core/models/dashboard.model';
import { AppLog } from '../../core/models/log.model';
import { OnboardingWizardComponent } from './onboarding-wizard/onboarding-wizard.component';

Chart.register(...registerables);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, OnboardingWizardComponent],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('incomeChart') incomeCanvasRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('breakdownChart') breakdownCanvasRef!: ElementRef<HTMLCanvasElement>;

  private auth = inject(AuthService);
  private api = inject(ApiService);
  private toast = inject(ToastService);

  user = this.auth.currentUser;
  isLandlord = computed(() => this.auth.currentUser()?.role === 'Landlord');

  private sessionSkipped = signal(false);
  showWizard = computed(() =>
    this.isLandlord() &&
    this.user()?.onboardingCompleted === false &&
    !this.sessionSkipped()
  );

  data: DashboardData | null = null;
  loading = true;
  recentLogs: AppLog[] = [];
  seedingDemo = false;

  private incomeChart: Chart | null = null;
  private breakdownChart: Chart | null = null;
  private chartsReady = false;
  private dataReady = false;

  ngOnInit() {
    if (!this.auth.currentUser()) {
      this.api.getMe().subscribe(user => this.auth.setCurrentUser(user));
    }
    this.api.getDashboard().subscribe({
      next: (d) => {
        this.data = d;
        this.loading = false;
        this.dataReady = true;
        if (this.chartsReady) this.renderCharts();
      },
      error: () => { this.loading = false; }
    });
    if (this.isLandlord()) {
      this.api.getLogs(undefined, 1, 5).subscribe({
        next: (r) => { this.recentLogs = r.items; },
        error: () => {}
      });
    }
  }

  ngAfterViewInit() {
    this.chartsReady = true;
    if (this.dataReady) this.renderCharts();
  }

  ngOnDestroy() {
    this.incomeChart?.destroy();
    this.breakdownChart?.destroy();
  }

  onWizardDismissed(completed: boolean) {
    this.sessionSkipped.set(true);
    if (completed) {
      this.api.getMe().subscribe(u => this.auth.setCurrentUser(u));
    }
  }

  loadDemoData() {
    if (!confirm('This will create sample properties, tenants, leases, and payments for demo purposes. Continue?')) return;
    this.seedingDemo = true;
    this.api.loadDemoData().subscribe({
      next: () => {
        this.toast.success('Demo data loaded! Refreshing…');
        this.seedingDemo = false;
        this.ngOnInit();
      },
      error: () => {
        this.toast.error('Failed to load demo data.');
        this.seedingDemo = false;
      }
    });
  }

  relativeTime(dateStr: string): string {
    const diff = Date.now() - new Date(dateStr).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 60) return `${Math.max(1, mins)}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return `${hrs}h ago`;
    return `${Math.floor(hrs / 24)}d ago`;
  }

  iconFor(entityType: string): string {
    const icons: Record<string, string> = {
      Property: '🏠', Lease: '📄', Payment: '💰', Maintenance: '🔧', Tenant: '👤'
    };
    return icons[entityType] ?? '📝';
  }

  private renderCharts() {
    if (!this.data) return;
    this.renderIncomeChart();
    this.renderBreakdownChart();
  }

  private renderIncomeChart() {
    if (!this.incomeCanvasRef || !this.data?.monthlyIncome.length) return;
    this.incomeChart?.destroy();
    const labels = this.data.monthlyIncome.map(p => {
      const [y, m] = p.month.split('-');
      return new Date(+y, +m - 1).toLocaleDateString('en-PH', { month: 'short', year: 'numeric' });
    });
    this.incomeChart = new Chart(this.incomeCanvasRef.nativeElement, {
      type: 'bar',
      data: {
        labels,
        datasets: [{
          label: 'Income (₱)',
          data: this.data.monthlyIncome.map(p => p.amount),
          backgroundColor: 'rgba(99, 102, 241, 0.7)',
          borderRadius: 4
        }]
      },
      options: {
        responsive: true,
        plugins: { legend: { display: false } },
        scales: { y: { beginAtZero: true, ticks: { callback: v => '₱' + Number(v).toLocaleString() } } }
      }
    });
  }

  private renderBreakdownChart() {
    if (!this.breakdownCanvasRef || !this.data) return;
    const { paid, partial, unpaid } = this.data.paymentBreakdown;
    if (paid + partial + unpaid === 0) return;
    this.breakdownChart?.destroy();
    this.breakdownChart = new Chart(this.breakdownCanvasRef.nativeElement, {
      type: 'doughnut',
      data: {
        labels: ['Paid', 'Partial', 'Unpaid'],
        datasets: [{
          data: [paid, partial, unpaid],
          backgroundColor: ['#22c55e', '#f59e0b', '#ef4444'],
          borderWidth: 2
        }]
      },
      options: {
        responsive: true,
        plugins: { legend: { position: 'bottom' } }
      }
    });
  }

  fmt(n: number) {
    return '₱' + n.toLocaleString('en-PH', { minimumFractionDigits: 2 });
  }
}
```

- [ ] **Step 2: Update `dashboard.component.html` — add seed banner, wizard render, and activity card**

Open the existing `dashboard.component.html`. Add these three blocks:

**a) Wizard render — add as first child inside the outermost wrapper div (or at the very top of the template):**

```html
@if (showWizard()) {
  <app-onboarding-wizard (dismissed)="onWizardDismissed($event)" />
}
```

**b) Seed data banner — add inside the `@else if (data)` block, before the metric-grid, only for landlords with 0 properties:**

```html
@if (isLandlord() && data.totalProperties === 0) {
  <div class="table-card" style="padding:14px 20px;margin-bottom:20px;display:flex;align-items:center;justify-content:space-between;gap:12px">
    <span style="font-size:14px;color:var(--color-text-muted)">No properties yet. Explore the app with sample data?</span>
    <button class="btn btn-secondary" style="font-size:13px;white-space:nowrap"
            (click)="loadDemoData()" [disabled]="seedingDemo">
      {{ seedingDemo ? 'Loading…' : 'Load Demo Data' }}
    </button>
  </div>
}
```

**c) Recent Activity card — add after the charts-grid, inside the landlord section:**

```html
@if (isLandlord() && recentLogs.length > 0) {
  <div class="table-card" style="margin-top:24px">
    <div style="padding:16px 20px;border-bottom:1px solid var(--color-border);display:flex;align-items:center;justify-content:space-between">
      <h2 style="font-size:16px;font-weight:600;margin:0">Recent Activity</h2>
      <a routerLink="/account/activity" class="btn btn-secondary" style="font-size:13px;padding:4px 12px">View all</a>
    </div>
    @for (log of recentLogs; track log.id) {
      <div style="padding:12px 20px;border-bottom:1px solid var(--color-border);display:flex;gap:12px;align-items:flex-start">
        <span style="font-size:18px;line-height:1.4">{{ iconFor(log.entityType) }}</span>
        <div>
          <div style="font-size:14px">{{ log.description }}</div>
          <div style="font-size:12px;color:var(--color-text-muted);margin-top:2px">{{ relativeTime(log.createdAt) }}</div>
        </div>
      </div>
    }
  </div>
}
```

- [ ] **Step 3: Commit (placeholder — OnboardingWizardComponent doesn't exist yet; build will fail until Task 10)**

Hold off on committing until Task 10 is complete.

---

## Task 10: Onboarding Wizard component

**Files:**
- Create: `frontend/rental-management-ui/src/app/features/dashboard/onboarding-wizard/onboarding-wizard.component.ts`
- Create: `frontend/rental-management-ui/src/app/features/dashboard/onboarding-wizard/onboarding-wizard.component.html`

- [ ] **Step 1: Create `onboarding-wizard.component.ts`**

```typescript
import { Component, OnInit, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-onboarding-wizard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './onboarding-wizard.component.html'
})
export class OnboardingWizardComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  @Output() dismissed = new EventEmitter<boolean>();

  step = signal(1);
  totalSteps = 4;
  saving = false;
  tourStep = signal(0);

  profileForm = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    phone: ['', Validators.maxLength(30)]
  });

  tourTooltips = [
    { title: 'Portfolio Overview', body: 'See your total properties, occupied vs vacant, and monthly rent at a glance.' },
    { title: 'Unpaid Rent Tracker', body: 'Track total outstanding rent across all your tenants instantly.' },
    { title: 'Maintenance Monitor', body: 'Stay on top of all open maintenance requests from your tenants.' }
  ];

  ngOnInit() {
    const saved = localStorage.getItem('onboarding_step');
    if (saved) this.step.set(parseInt(saved, 10));

    const user = this.auth.currentUser();
    if (user) {
      this.profileForm.patchValue({
        firstName: user.firstName,
        lastName: user.lastName,
        phone: user.phone ?? ''
      });
    }
  }

  advance() {
    const next = this.step() + 1;
    this.step.set(next);
    localStorage.setItem('onboarding_step', String(next));
  }

  saveProfile() {
    if (this.profileForm.invalid) { this.profileForm.markAllAsTouched(); return; }
    this.saving = true;
    const v = this.profileForm.value;
    this.api.updateMe({ firstName: v.firstName!, lastName: v.lastName!, phone: v.phone || undefined }).subscribe({
      next: (u) => {
        this.auth.setCurrentUser(u);
        this.saving = false;
        this.advance();
      },
      error: () => {
        this.toast.error('Failed to save profile.');
        this.saving = false;
      }
    });
  }

  goToProperty() {
    localStorage.setItem('onboarding_step', '2');
    this.router.navigate(['/properties/new']);
  }

  goToLease() {
    localStorage.setItem('onboarding_step', '3');
    this.router.navigate(['/leases/new']);
  }

  nextTourStep() {
    const next = this.tourStep() + 1;
    if (next > this.tourTooltips.length) {
      this.completeTour();
    } else {
      this.tourStep.set(next);
    }
  }

  completeTour() {
    this.api.updateMe({ onboardingCompleted: true }).subscribe({
      next: (u) => {
        this.auth.setCurrentUser(u);
        localStorage.removeItem('onboarding_step');
        this.dismissed.emit(true);
      },
      error: () => {
        localStorage.removeItem('onboarding_step');
        this.dismissed.emit(true);
      }
    });
  }

  skip() {
    localStorage.removeItem('onboarding_step');
    this.dismissed.emit(false);
  }

  hasError(field: string) {
    const ctrl = this.profileForm.get(field);
    return ctrl?.invalid && ctrl?.touched;
  }
}
```

- [ ] **Step 2: Create `onboarding-wizard.component.html`**

```html
<!-- Full-screen overlay -->
<div style="position:fixed;inset:0;background:rgba(0,0,0,0.5);z-index:1000;display:flex;align-items:center;justify-content:center;padding:16px">
  <div style="background:var(--color-bg,#fff);border-radius:12px;width:100%;max-width:520px;box-shadow:0 20px 60px rgba(0,0,0,0.3)">

    <!-- Header -->
    <div style="padding:24px 28px 0">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:8px">
        <span style="font-size:12px;font-weight:600;color:var(--color-text-muted);text-transform:uppercase;letter-spacing:.05em">
          Step {{ step() }} of {{ totalSteps }}
        </span>
        <button type="button" (click)="skip()"
                style="background:none;border:none;cursor:pointer;font-size:13px;color:var(--color-text-muted);padding:0">
          Skip for now
        </button>
      </div>
      <!-- Progress bar -->
      <div style="height:4px;background:var(--color-border,#e5e7eb);border-radius:2px;overflow:hidden">
        <div [style.width.%]="(step() / totalSteps) * 100"
             style="height:100%;background:var(--color-primary,#6366f1);transition:width 0.3s ease"></div>
      </div>
    </div>

    <!-- Step content -->
    <div style="padding:24px 28px 28px">

      <!-- Step 1: Profile -->
      @if (step() === 1) {
        <h2 style="font-size:20px;font-weight:700;margin-bottom:8px">Welcome! Let's set up your profile.</h2>
        <p style="font-size:14px;color:var(--color-text-muted);margin-bottom:20px">
          Your name and contact details help tenants identify you.
        </p>
        <form [formGroup]="profileForm" (ngSubmit)="saveProfile()">
          <div class="form-row">
            <div class="form-group">
              <label>First Name *</label>
              <input formControlName="firstName" type="text" />
              @if (hasError('firstName')) { <span class="field-error">Required.</span> }
            </div>
            <div class="form-group">
              <label>Last Name *</label>
              <input formControlName="lastName" type="text" />
              @if (hasError('lastName')) { <span class="field-error">Required.</span> }
            </div>
          </div>
          <div class="form-group">
            <label>Phone (optional)</label>
            <input formControlName="phone" type="tel" placeholder="+63 900 000 0000" />
          </div>
          <button type="submit" class="btn btn-primary" style="width:100%;margin-top:8px" [disabled]="saving">
            {{ saving ? 'Saving…' : 'Save & Continue →' }}
          </button>
        </form>
      }

      <!-- Step 2: Add Property -->
      @if (step() === 2) {
        <h2 style="font-size:20px;font-weight:700;margin-bottom:8px">Add your first property.</h2>
        <p style="font-size:14px;color:var(--color-text-muted);margin-bottom:20px">
          Properties are the foundation of your portfolio. Add a property to start managing leases and rent.
        </p>
        <button type="button" class="btn btn-primary" style="width:100%;margin-bottom:12px" (click)="goToProperty()">
          Create Property →
        </button>
        <button type="button" class="btn btn-secondary" style="width:100%" (click)="advance()">
          I've already added one →
        </button>
      }

      <!-- Step 3: Create Lease -->
      @if (step() === 3) {
        <h2 style="font-size:20px;font-weight:700;margin-bottom:8px">Create your first lease.</h2>
        <p style="font-size:14px;color:var(--color-text-muted);margin-bottom:20px">
          Assign a tenant to a property and the system will automatically schedule monthly rent payments.
        </p>
        <button type="button" class="btn btn-primary" style="width:100%;margin-bottom:12px" (click)="goToLease()">
          Create Lease →
        </button>
        <button type="button" class="btn btn-secondary" style="width:100%" (click)="advance()">
          I've already created one →
        </button>
      }

      <!-- Step 4: Dashboard Tour -->
      @if (step() === 4) {
        <h2 style="font-size:20px;font-weight:700;margin-bottom:8px">Explore your dashboard.</h2>
        <p style="font-size:14px;color:var(--color-text-muted);margin-bottom:20px">
          Here's a quick tour of the key features on your dashboard.
        </p>

        @if (tourStep() === 0) {
          <div style="text-align:center;padding:16px 0">
            <p style="font-size:14px;color:var(--color-text-muted);margin-bottom:20px">
              You're all set! Click "Start Tour" to walk through the dashboard highlights, or finish now.
            </p>
            <button type="button" class="btn btn-secondary" style="margin-right:12px" (click)="nextTourStep()">
              Start Tour
            </button>
            <button type="button" class="btn btn-primary" (click)="completeTour()">
              Finish Setup
            </button>
          </div>
        } @else {
          <div style="background:var(--color-bg-subtle,#f8f9fa);border-radius:8px;padding:16px;margin-bottom:16px">
            <div style="font-size:12px;font-weight:600;color:var(--color-primary,#6366f1);margin-bottom:4px">
              {{ tourStep() }} / {{ tourTooltips.length }}
            </div>
            <div style="font-size:15px;font-weight:600;margin-bottom:4px">
              {{ tourTooltips[tourStep() - 1].title }}
            </div>
            <div style="font-size:13px;color:var(--color-text-muted)">
              {{ tourTooltips[tourStep() - 1].body }}
            </div>
          </div>
          <button type="button" class="btn btn-primary" style="width:100%" (click)="nextTourStep()">
            {{ tourStep() < tourTooltips.length ? 'Next →' : 'Finish Tour ✓' }}
          </button>
        }
      }

    </div>
  </div>
</div>
```

- [ ] **Step 3: Build Angular to verify no errors**

```bash
cd frontend/rental-management-ui && npx ng build --configuration development 2>&1 | tail -20
```

Expected: Build completed successfully.

- [ ] **Step 4: Commit dashboard + wizard together**

```bash
git add frontend/rental-management-ui/src/app/features/dashboard/ 
git commit -m "feat(onboarding): add 4-step onboarding wizard and wire into dashboard"
```

---

## Task 11: Activity Feed page

**Files:**
- Create: `frontend/rental-management-ui/src/app/features/account/activity/activity.component.ts`
- Create: `frontend/rental-management-ui/src/app/features/account/activity/activity.component.html`
- Modify: `frontend/rental-management-ui/src/app/features/account/account.routes.ts`

- [ ] **Step 1: Create `activity.component.ts`**

```typescript
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { AppLog } from '../../../core/models/log.model';

@Component({
  selector: 'app-activity',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './activity.component.html'
})
export class ActivityComponent implements OnInit {
  private api = inject(ApiService);

  logs: AppLog[] = [];
  loading = true;
  totalCount = 0;
  page = 1;
  readonly pageSize = 20;
  selectedType = '';

  readonly entityTypes = [
    { value: '', label: 'All' },
    { value: 'Property', label: 'Properties' },
    { value: 'Lease', label: 'Leases' },
    { value: 'Payment', label: 'Payments' },
    { value: 'Maintenance', label: 'Maintenance' }
  ];

  ngOnInit() { this.loadLogs(true); }

  loadLogs(reset = false) {
    if (reset) { this.page = 1; this.logs = []; }
    this.loading = true;
    this.api.getLogs(this.selectedType || undefined, this.page, this.pageSize).subscribe({
      next: (r) => {
        this.logs = reset ? r.items : [...this.logs, ...r.items];
        this.totalCount = r.totalCount;
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }

  filterByType(value: string) {
    this.selectedType = value;
    this.loadLogs(true);
  }

  loadMore() {
    this.page++;
    this.loadLogs(false);
  }

  get hasMore(): boolean {
    return this.logs.length < this.totalCount;
  }

  relativeTime(dateStr: string): string {
    const diff = Date.now() - new Date(dateStr).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 60) return `${Math.max(1, mins)}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return `${hrs}h ago`;
    return `${Math.floor(hrs / 24)}d ago`;
  }

  iconFor(entityType: string): string {
    const icons: Record<string, string> = {
      Property: '🏠', Lease: '📄', Payment: '💰', Maintenance: '🔧', Tenant: '👤'
    };
    return icons[entityType] ?? '📝';
  }
}
```

- [ ] **Step 2: Create `activity.component.html`**

```html
<a routerLink="/dashboard" class="back-link">← Back to Dashboard</a>

<div class="page-header">
  <h1>Activity Feed</h1>
  <p class="page-subtitle">A log of all actions across your portfolio</p>
</div>

<!-- Filter bar -->
<div style="display:flex;gap:8px;flex-wrap:wrap;margin-bottom:20px">
  @for (type of entityTypes; track type.value) {
    <button class="btn"
            [class.btn-primary]="selectedType === type.value"
            [class.btn-secondary]="selectedType !== type.value"
            style="font-size:13px;padding:4px 14px"
            (click)="filterByType(type.value)">
      {{ type.label }}
    </button>
  }
</div>

@if (loading && logs.length === 0) {
  <div class="empty-state">Loading…</div>
} @else if (logs.length === 0) {
  <div class="empty-state">No activity yet.</div>
} @else {
  <div class="table-card">
    @for (log of logs; track log.id) {
      <div style="padding:14px 20px;border-bottom:1px solid var(--color-border);display:flex;gap:14px;align-items:flex-start">
        <span style="font-size:20px;line-height:1.3;flex-shrink:0">{{ iconFor(log.entityType) }}</span>
        <div style="flex:1;min-width:0">
          <div style="font-size:14px">{{ log.description }}</div>
          <div style="font-size:12px;color:var(--color-text-muted);margin-top:2px">
            {{ log.entityType }} · {{ relativeTime(log.createdAt) }}
          </div>
        </div>
      </div>
    }
  </div>

  @if (hasMore) {
    <div style="text-align:center;margin-top:16px">
      <button class="btn btn-secondary" (click)="loadMore()" [disabled]="loading">
        {{ loading ? 'Loading…' : 'Load more' }}
      </button>
    </div>
  }
}
```

- [ ] **Step 3: Add route to `account.routes.ts`**

The existing file has routes for `''` (profile) and `'provision-templates'`. Add the activity route:

```typescript
{ path: 'activity', canActivate: [landlordGuard], loadComponent: () => import('./activity/activity.component').then(m => m.ActivityComponent) }
```

The full routes array should look like:

```typescript
export const accountRoutes: Routes = [
  { path: '', canActivate: [authGuard], loadComponent: () => import('./profile/profile.component').then(m => m.ProfileComponent) },
  { path: 'provision-templates', canActivate: [landlordGuard], loadComponent: () => import('./provision-templates/provision-templates.component').then(m => m.ProvisionTemplatesComponent) },
  { path: 'activity', canActivate: [landlordGuard], loadComponent: () => import('./activity/activity.component').then(m => m.ActivityComponent) }
];
```

- [ ] **Step 4: Build Angular**

```bash
cd frontend/rental-management-ui && npx ng build --configuration development 2>&1 | tail -20
```

Expected: Build completed successfully.

- [ ] **Step 5: Commit**

```bash
git add frontend/rental-management-ui/src/app/features/account/
git commit -m "feat(logs): add Activity Feed page at /account/activity"
```

---

## Task 12: Final verification and merge

- [ ] **Step 1: Run backend tests**

```bash
cd backend && dotnet test
```

Expected: All tests pass.

- [ ] **Step 2: Run Angular build (production)**

```bash
cd frontend/rental-management-ui && npx ng build 2>&1 | tail -20
```

Expected: Build succeeded with no errors.

- [ ] **Step 3: Manual smoke test (start backend + frontend)**

Start backend:
```bash
cd backend/RentalManagementApi && dotnet run
```

Start frontend:
```bash
cd frontend/rental-management-ui && npx ng serve
```

Verify:
1. Log in as a landlord → wizard appears (if `onboardingCompleted = false`)
2. Complete wizard step 1 (save profile) → step 2 appears
3. Click "Skip for now" → wizard dismissed for session
4. Dashboard shows "Load Demo Data" banner if 0 properties
5. Click "Load Demo Data" → confirm → banner disappears, metrics update
6. Navigate to `/account/activity` → feed shows logged entries with entity type filters
7. Dashboard shows "Recent Activity" card with last 5 entries

- [ ] **Step 4: Merge to main**

```bash
git checkout main
git merge feature/backfill-audit-onboarding-seed
git push origin main
```
