# Lease Special Provisions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow landlords to build a reusable clause library and attach special provisions to leases, which appear as a formatted "Special Provisions" section in the generated PDF.

**Architecture:** Two new EF Core entities (`ProvisionTemplate` for the landlord's library, `LeaseProvision` for per-lease snapshots) backed by a single `ProvisionService`. Four new API endpoints (templates CRUD + lease provisions CRUD). PdfService eager-loads provisions and conditionally renders the section. Angular adds a Provision Templates management page, a provisions picker in the lease form, and a read/edit provisions card in the lease detail.

**Tech Stack:** C# / .NET 8, Entity Framework Core, QuestPDF, Angular 17+ standalone components, TypeScript, SCSS.

---

## File Map

**Create (backend):**
- `backend/RentalManagementApi/Entities/ProvisionTemplate.cs`
- `backend/RentalManagementApi/Entities/LeaseProvision.cs`
- `backend/RentalManagementApi/DTOs/Provisions/ProvisionDto.cs`
- `backend/RentalManagementApi/Application/Services/ProvisionService.cs`
- `backend/RentalManagementApi/Controllers/ProvisionTemplatesController.cs`
- `backend/RentalManagementApi/Controllers/LeaseProvisionsController.cs`

**Modify (backend):**
- `backend/RentalManagementApi/Entities/Lease.cs` — add `Provisions` nav property
- `backend/RentalManagementApi/Data/AppDbContext.cs` — add DbSets + model config
- `backend/RentalManagementApi/Application/Services/PdfService.cs` — include provisions in PDF
- `backend/RentalManagementApi/Program.cs` — register `ProvisionService`

**Create (frontend):**
- `frontend/rental-management-ui/src/app/core/models/provision.model.ts`
- `frontend/rental-management-ui/src/app/features/account/provision-templates/provision-templates.component.ts`
- `frontend/rental-management-ui/src/app/features/account/provision-templates/provision-templates.component.html`

**Modify (frontend):**
- `frontend/rental-management-ui/src/app/core/services/api.service.ts` — add 7 new methods
- `frontend/rental-management-ui/src/app/features/account/account.routes.ts` — add route
- `frontend/rental-management-ui/src/app/app.html` — add sidebar nav item
- `frontend/rental-management-ui/src/app/features/leases/lease-form/lease-form.component.ts`
- `frontend/rental-management-ui/src/app/features/leases/lease-form/lease-form.component.html`
- `frontend/rental-management-ui/src/app/features/leases/lease-detail/lease-detail.component.ts`
- `frontend/rental-management-ui/src/app/features/leases/lease-detail/lease-detail.component.html`

---

## Task 1: Backend entities

**Files:**
- Create: `backend/RentalManagementApi/Entities/ProvisionTemplate.cs`
- Create: `backend/RentalManagementApi/Entities/LeaseProvision.cs`
- Modify: `backend/RentalManagementApi/Entities/Lease.cs`

- [ ] **Step 1: Create `ProvisionTemplate.cs`**

```csharp
namespace RentalManagementApi.Entities;

public class ProvisionTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LandlordId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Landlord { get; set; } = null!;
}
```

- [ ] **Step 2: Create `LeaseProvision.cs`**

```csharp
namespace RentalManagementApi.Entities;

public class LeaseProvision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LeaseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Lease Lease { get; set; } = null!;
}
```

- [ ] **Step 3: Add `Provisions` navigation property to `Lease.cs`**

Current file ends with:
```csharp
    public ICollection<Payment> Payments { get; set; } = [];
}
```

Replace that closing section with:
```csharp
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<LeaseProvision> Provisions { get; set; } = [];
}
```

- [ ] **Step 4: Commit**

```
git add backend/RentalManagementApi/Entities/ProvisionTemplate.cs
git add backend/RentalManagementApi/Entities/LeaseProvision.cs
git add backend/RentalManagementApi/Entities/Lease.cs
git commit -m "feat(provisions): add ProvisionTemplate and LeaseProvision entities"
```

---

## Task 2: Update AppDbContext and run EF migration

**Files:**
- Modify: `backend/RentalManagementApi/Data/AppDbContext.cs`

- [ ] **Step 1: Add DbSets to `AppDbContext`**

After line `public DbSet<ReminderLog> ReminderLogs => Set<ReminderLog>();`, add:

```csharp
    public DbSet<ProvisionTemplate> ProvisionTemplates => Set<ProvisionTemplate>();
    public DbSet<LeaseProvision> LeaseProvisions => Set<LeaseProvision>();
```

- [ ] **Step 2: Add model configuration to `OnModelCreating`**

After the `ReminderLog` configuration block (before the closing `}`), add:

```csharp
        modelBuilder.Entity<ProvisionTemplate>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).IsRequired().HasMaxLength(200);
            e.Property(t => t.Body).IsRequired().HasMaxLength(4000);
            e.HasIndex(t => t.LandlordId);
            e.HasOne(t => t.Landlord)
             .WithMany()
             .HasForeignKey(t => t.LandlordId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LeaseProvision>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Title).IsRequired().HasMaxLength(200);
            e.Property(p => p.Body).IsRequired().HasMaxLength(4000);
            e.HasIndex(p => p.LeaseId);
            e.HasOne(p => p.Lease)
             .WithMany(l => l.Provisions)
             .HasForeignKey(p => p.LeaseId)
             .OnDelete(DeleteBehavior.Cascade);
        });
```

- [ ] **Step 3: Generate the EF migration**

Run from `backend/RentalManagementApi/`:

```
dotnet ef migrations add AddProvisions
```

Expected: new files created in `Data/Migrations/` — one `AddProvisions.cs` and one `AddProvisions.Designer.cs`.

- [ ] **Step 4: Apply the migration to the database**

```
dotnet ef database update
```

Expected output ends with `Done.`

- [ ] **Step 5: Commit**

```
git add backend/RentalManagementApi/Data/AppDbContext.cs
git add backend/RentalManagementApi/Data/Migrations/
git commit -m "feat(provisions): add EF migration for ProvisionTemplates and LeaseProvisions"
```

---

## Task 3: Backend DTOs

**Files:**
- Create: `backend/RentalManagementApi/DTOs/Provisions/ProvisionDto.cs`

- [ ] **Step 1: Create `ProvisionDto.cs`**

```csharp
namespace RentalManagementApi.DTOs.Provisions;

public class ProvisionTemplateDto
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class LeaseProvisionDto
{
    public Guid Id { get; set; }
    public Guid LeaseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProvisionTemplateRequest
{
    public required string Title { get; set; }
    public required string Body { get; set; }
}

public class UpdateProvisionTemplateRequest
{
    public required string Title { get; set; }
    public required string Body { get; set; }
}

public class LeaseProvisionPayload
{
    public required string Title { get; set; }
    public required string Body { get; set; }
    public int SortOrder { get; set; }
}
```

- [ ] **Step 2: Commit**

```
git add backend/RentalManagementApi/DTOs/Provisions/ProvisionDto.cs
git commit -m "feat(provisions): add provision DTOs"
```

---

## Task 4: ProvisionService

**Files:**
- Create: `backend/RentalManagementApi/Application/Services/ProvisionService.cs`

- [ ] **Step 1: Create `ProvisionService.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using RentalManagementApi.Data;
using RentalManagementApi.DTOs.Provisions;
using RentalManagementApi.Entities;

namespace RentalManagementApi.Application.Services;

public class ProvisionService(AppDbContext db)
{
    // --- Templates ---

    public async Task<List<ProvisionTemplateDto>> GetTemplatesAsync(Guid landlordId)
    {
        return await db.ProvisionTemplates
            .Where(t => t.LandlordId == landlordId)
            .OrderBy(t => t.CreatedAt)
            .Select(t => ToTemplateDto(t))
            .ToListAsync();
    }

    public async Task<ProvisionTemplateDto> CreateTemplateAsync(Guid landlordId, CreateProvisionTemplateRequest request)
    {
        var template = new ProvisionTemplate
        {
            LandlordId = landlordId,
            Title = request.Title.Trim(),
            Body = request.Body.Trim()
        };
        db.ProvisionTemplates.Add(template);
        await db.SaveChangesAsync();
        return ToTemplateDto(template);
    }

    public async Task<ProvisionTemplateDto?> UpdateTemplateAsync(Guid id, Guid landlordId, UpdateProvisionTemplateRequest request)
    {
        var template = await db.ProvisionTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.LandlordId == landlordId);
        if (template is null) return null;

        template.Title = request.Title.Trim();
        template.Body = request.Body.Trim();
        await db.SaveChangesAsync();
        return ToTemplateDto(template);
    }

    public async Task<bool> DeleteTemplateAsync(Guid id, Guid landlordId)
    {
        var template = await db.ProvisionTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.LandlordId == landlordId);
        if (template is null) return false;

        db.ProvisionTemplates.Remove(template);
        await db.SaveChangesAsync();
        return true;
    }

    // --- Lease Provisions ---

    public async Task<List<LeaseProvisionDto>> GetLeaseProvisionsAsync(Guid leaseId, Guid userId, string role)
    {
        var hasAccess = role == "Landlord"
            ? await db.Leases.AnyAsync(l => l.Id == leaseId && l.Property.LandlordId == userId)
            : await db.Leases.AnyAsync(l => l.Id == leaseId && l.TenantId == userId);

        if (!hasAccess) throw new UnauthorizedAccessException("Access denied.");

        return await db.LeaseProvisions
            .Where(p => p.LeaseId == leaseId)
            .OrderBy(p => p.SortOrder)
            .Select(p => ToProvisionDto(p))
            .ToListAsync();
    }

    public async Task<List<LeaseProvisionDto>> SetLeaseProvisionsAsync(
        Guid leaseId, Guid landlordId, List<LeaseProvisionPayload> payloads)
    {
        var leaseExists = await db.Leases
            .AnyAsync(l => l.Id == leaseId && l.Property.LandlordId == landlordId);
        if (!leaseExists) throw new KeyNotFoundException("Lease not found.");

        var existing = await db.LeaseProvisions.Where(p => p.LeaseId == leaseId).ToListAsync();
        db.LeaseProvisions.RemoveRange(existing);

        var newProvisions = payloads.Select((p, idx) => new LeaseProvision
        {
            LeaseId = leaseId,
            Title = p.Title.Trim(),
            Body = p.Body.Trim(),
            SortOrder = p.SortOrder != 0 ? p.SortOrder : idx
        }).ToList();

        db.LeaseProvisions.AddRange(newProvisions);
        await db.SaveChangesAsync();

        return newProvisions.Select(ToProvisionDto).ToList();
    }

    public async Task<bool> DeleteLeaseProvisionAsync(Guid leaseId, Guid provisionId, Guid landlordId)
    {
        var provision = await db.LeaseProvisions
            .Include(p => p.Lease).ThenInclude(l => l.Property)
            .FirstOrDefaultAsync(p =>
                p.Id == provisionId &&
                p.LeaseId == leaseId &&
                p.Lease.Property.LandlordId == landlordId);
        if (provision is null) return false;

        db.LeaseProvisions.Remove(provision);
        await db.SaveChangesAsync();
        return true;
    }

    private static ProvisionTemplateDto ToTemplateDto(ProvisionTemplate t) => new()
    {
        Id = t.Id,
        LandlordId = t.LandlordId,
        Title = t.Title,
        Body = t.Body,
        CreatedAt = t.CreatedAt
    };

    private static LeaseProvisionDto ToProvisionDto(LeaseProvision p) => new()
    {
        Id = p.Id,
        LeaseId = p.LeaseId,
        Title = p.Title,
        Body = p.Body,
        SortOrder = p.SortOrder,
        CreatedAt = p.CreatedAt
    };
}
```

- [ ] **Step 2: Register in `Program.cs`**

After line `builder.Services.AddScoped<PdfService>();`, add:

```csharp
builder.Services.AddScoped<ProvisionService>();
```

- [ ] **Step 3: Commit**

```
git add backend/RentalManagementApi/Application/Services/ProvisionService.cs
git add backend/RentalManagementApi/Program.cs
git commit -m "feat(provisions): add ProvisionService"
```

---

## Task 5: ProvisionTemplatesController

**Files:**
- Create: `backend/RentalManagementApi/Controllers/ProvisionTemplatesController.cs`

- [ ] **Step 1: Create the controller**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Common;
using RentalManagementApi.DTOs.Provisions;

namespace RentalManagementApi.Controllers;

[ApiController]
[Route("api/provision-templates")]
[Authorize(Policy = "LandlordPolicy")]
public class ProvisionTemplatesController(ProvisionService provisionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var templates = await provisionService.GetTemplatesAsync(landlordId.Value);
        return Ok(templates);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProvisionTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new ApiError("Title and body are required.", "INVALID_INPUT"));

        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var template = await provisionService.CreateTemplateAsync(landlordId.Value, request);
        return Ok(template);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProvisionTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new ApiError("Title and body are required.", "INVALID_INPUT"));

        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var template = await provisionService.UpdateTemplateAsync(id, landlordId.Value, request);
        if (template is null) return NotFound(new ApiError("Template not found.", "TEMPLATE_NOT_FOUND"));

        return Ok(template);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var deleted = await provisionService.DeleteTemplateAsync(id, landlordId.Value);
        if (!deleted) return NotFound(new ApiError("Template not found.", "TEMPLATE_NOT_FOUND"));

        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
```

- [ ] **Step 2: Commit**

```
git add backend/RentalManagementApi/Controllers/ProvisionTemplatesController.cs
git commit -m "feat(provisions): add ProvisionTemplatesController"
```

---

## Task 6: LeaseProvisionsController

**Files:**
- Create: `backend/RentalManagementApi/Controllers/LeaseProvisionsController.cs`

- [ ] **Step 1: Create the controller**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Common;
using RentalManagementApi.DTOs.Provisions;

namespace RentalManagementApi.Controllers;

[ApiController]
[Route("api/leases/{leaseId:guid}/provisions")]
[Authorize]
public class LeaseProvisionsController(ProvisionService provisionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid leaseId)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (userId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        try
        {
            var provisions = await provisionService.GetLeaseProvisionsAsync(leaseId, userId.Value, role);
            return Ok(provisions);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut]
    [Authorize(Policy = "LandlordPolicy")]
    public async Task<IActionResult> Set(Guid leaseId, [FromBody] List<LeaseProvisionPayload> payloads)
    {
        if (payloads.Any(p => string.IsNullOrWhiteSpace(p.Title) || string.IsNullOrWhiteSpace(p.Body)))
            return BadRequest(new ApiError("Each provision must have a title and body.", "INVALID_INPUT"));

        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        try
        {
            var provisions = await provisionService.SetLeaseProvisionsAsync(leaseId, landlordId.Value, payloads);
            return Ok(provisions);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiError("Lease not found.", "LEASE_NOT_FOUND"));
        }
    }

    [HttpDelete("{provisionId:guid}")]
    [Authorize(Policy = "LandlordPolicy")]
    public async Task<IActionResult> Delete(Guid leaseId, Guid provisionId)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var deleted = await provisionService.DeleteLeaseProvisionAsync(leaseId, provisionId, landlordId.Value);
        if (!deleted) return NotFound(new ApiError("Provision not found.", "PROVISION_NOT_FOUND"));

        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private string GetCurrentUserRole() =>
        User.FindFirst("user_role")?.Value ?? string.Empty;
}
```

- [ ] **Step 2: Verify the backend builds**

Run from `backend/RentalManagementApi/`:

```
dotnet build
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```
git add backend/RentalManagementApi/Controllers/LeaseProvisionsController.cs
git commit -m "feat(provisions): add LeaseProvisionsController"
```

---

## Task 7: Update PdfService to render provisions

**Files:**
- Modify: `backend/RentalManagementApi/Application/Services/PdfService.cs`

- [ ] **Step 1: Update the lease query to include provisions**

In `GenerateLeasePdfAsync`, replace the existing `db.Leases` query (lines 14–17):

```csharp
var lease = await db.Leases
    .Include(l => l.Property).ThenInclude(p => p.Landlord)
    .Include(l => l.Tenant)
    .FirstOrDefaultAsync(l => l.Id == leaseId);
```

With:

```csharp
var lease = await db.Leases
    .Include(l => l.Property).ThenInclude(p => p.Landlord)
    .Include(l => l.Tenant)
    .Include(l => l.Provisions.OrderBy(p => p.SortOrder))
    .FirstOrDefaultAsync(l => l.Id == leaseId);
```

- [ ] **Step 2: Add the Special Provisions section to the PDF**

In the `page.Content()` column, find the separator line after "Lease Terms":

```csharp
                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                    col.Item().Text("Signatures").Bold().FontSize(13).FontColor(Colors.Grey.Darken2);
```

Replace it with:

```csharp
                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                    if (lease.Provisions.Any())
                    {
                        col.Item().Text("Special Provisions").Bold().FontSize(13).FontColor(Colors.Grey.Darken2);

                        foreach (var provision in lease.Provisions)
                        {
                            col.Item().Column(inner =>
                            {
                                inner.Item().PaddingBottom(2).Text(provision.Title).SemiBold();
                                inner.Item().Text(provision.Body).FontSize(10).FontColor(Colors.Grey.Darken1);
                            });
                        }

                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    }

                    col.Item().Text("Signatures").Bold().FontSize(13).FontColor(Colors.Grey.Darken2);
```

- [ ] **Step 3: Verify the backend still builds**

```
dotnet build
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```
git add backend/RentalManagementApi/Application/Services/PdfService.cs
git commit -m "feat(provisions): render special provisions section in lease PDF"
```

---

## Task 8: Frontend models and ApiService

**Files:**
- Create: `frontend/rental-management-ui/src/app/core/models/provision.model.ts`
- Modify: `frontend/rental-management-ui/src/app/core/services/api.service.ts`

- [ ] **Step 1: Create `provision.model.ts`**

```typescript
export interface ProvisionTemplate {
  id: string;
  landlordId: string;
  title: string;
  body: string;
  createdAt: string;
}

export interface LeaseProvision {
  id: string;
  leaseId: string;
  title: string;
  body: string;
  sortOrder: number;
  createdAt: string;
}

export interface LeaseProvisionPayload {
  title: string;
  body: string;
  sortOrder: number;
}
```

- [ ] **Step 2: Add imports to `api.service.ts`**

At the top of the file, add the new import after the existing imports:

```typescript
import { ProvisionTemplate, LeaseProvision, LeaseProvisionPayload } from '../models/provision.model';
```

- [ ] **Step 3: Add provision methods to `ApiService`**

At the end of the class (before the closing `}`), add:

```typescript
  // --- Provision Templates ---

  getProvisionTemplates() {
    return this.http.get<ProvisionTemplate[]>(`${this.base}/provision-templates`);
  }

  createProvisionTemplate(payload: { title: string; body: string }) {
    return this.http.post<ProvisionTemplate>(`${this.base}/provision-templates`, payload);
  }

  updateProvisionTemplate(id: string, payload: { title: string; body: string }) {
    return this.http.put<ProvisionTemplate>(`${this.base}/provision-templates/${id}`, payload);
  }

  deleteProvisionTemplate(id: string) {
    return this.http.delete<void>(`${this.base}/provision-templates/${id}`);
  }

  // --- Lease Provisions ---

  getLeaseProvisions(leaseId: string) {
    return this.http.get<LeaseProvision[]>(`${this.base}/leases/${leaseId}/provisions`);
  }

  setLeaseProvisions(leaseId: string, provisions: LeaseProvisionPayload[]) {
    return this.http.put<LeaseProvision[]>(`${this.base}/leases/${leaseId}/provisions`, provisions);
  }

  deleteLeaseProvision(leaseId: string, provisionId: string) {
    return this.http.delete<void>(`${this.base}/leases/${leaseId}/provisions/${provisionId}`);
  }
```

- [ ] **Step 4: Commit**

```
git add frontend/rental-management-ui/src/app/core/models/provision.model.ts
git add frontend/rental-management-ui/src/app/core/services/api.service.ts
git commit -m "feat(provisions): add provision models and ApiService methods"
```

---

## Task 9: Account — Provision Templates management page

**Files:**
- Create: `frontend/rental-management-ui/src/app/features/account/provision-templates/provision-templates.component.ts`
- Create: `frontend/rental-management-ui/src/app/features/account/provision-templates/provision-templates.component.html`
- Modify: `frontend/rental-management-ui/src/app/features/account/account.routes.ts`
- Modify: `frontend/rental-management-ui/src/app/app.html`

- [ ] **Step 1: Create `provision-templates.component.ts`**

```typescript
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { ProvisionTemplate } from '../../../core/models/provision.model';

@Component({
  selector: 'app-provision-templates',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './provision-templates.component.html'
})
export class ProvisionTemplatesComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  templates: ProvisionTemplate[] = [];
  loading = true;
  saving = false;
  editingId: string | null = null;

  form: FormGroup = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    body: ['', [Validators.required, Validators.maxLength(4000)]]
  });

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.api.getProvisionTemplates().subscribe({
      next: (t) => { this.templates = t; this.loading = false; },
      error: () => { this.toast.error('Failed to load templates.'); this.loading = false; }
    });
  }

  startEdit(t: ProvisionTemplate) {
    this.editingId = t.id;
    this.form.patchValue({ title: t.title, body: t.body });
  }

  cancelEdit() {
    this.editingId = null;
    this.form.reset();
  }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving = true;
    const v = this.form.value;
    const req = this.editingId
      ? this.api.updateProvisionTemplate(this.editingId, v)
      : this.api.createProvisionTemplate(v);

    req.subscribe({
      next: () => {
        this.toast.success(this.editingId ? 'Template updated.' : 'Template saved.');
        this.editingId = null;
        this.form.reset();
        this.load();
      },
      error: () => this.toast.error('Failed to save template.'),
      complete: () => setTimeout(() => this.saving = false)
    });
  }

  delete(id: string) {
    if (!confirm('Delete this provision template? Existing lease provisions are not affected.')) return;
    this.api.deleteProvisionTemplate(id).subscribe({
      next: () => { this.toast.success('Template deleted.'); this.load(); },
      error: () => this.toast.error('Failed to delete template.')
    });
  }

  hasError(field: string) {
    const ctrl = this.form.get(field);
    return ctrl?.invalid && ctrl?.touched;
  }
}
```

- [ ] **Step 2: Create `provision-templates.component.html`**

```html
<div class="page-header">
  <h1>Provision Templates</h1>
  <p class="page-subtitle">Reusable contract clauses for your leases</p>
</div>

<div style="max-width:720px">
  <div class="table-card" style="padding:24px;margin-bottom:24px">
    <h2 style="font-size:15px;font-weight:600;margin-bottom:16px">
      {{ editingId ? 'Edit Template' : 'New Template' }}
    </h2>
    <form [formGroup]="form" (ngSubmit)="save()">
      <div class="form-group">
        <label>Title *</label>
        <input formControlName="title" type="text" placeholder="e.g. No Pets Policy" />
        @if (hasError('title')) { <span class="field-error">Title is required (max 200 chars).</span> }
      </div>
      <div class="form-group">
        <label>Body *</label>
        <textarea formControlName="body" rows="4" placeholder="Enter the full clause text…" style="width:100%;resize:vertical"></textarea>
        @if (hasError('body')) { <span class="field-error">Body is required (max 4000 chars).</span> }
      </div>
      <div style="display:flex;gap:12px">
        <button type="submit" class="btn btn-primary" [disabled]="saving">
          {{ saving ? 'Saving…' : (editingId ? 'Update Template' : 'Save Template') }}
        </button>
        @if (editingId) {
          <button type="button" class="btn btn-secondary" (click)="cancelEdit()">Cancel</button>
        }
      </div>
    </form>
  </div>

  @if (loading) {
    <div class="loading">Loading…</div>
  } @else if (templates.length === 0) {
    <div class="empty-state">
      <p>No provision templates yet. Add one above to reuse it across leases.</p>
    </div>
  } @else {
    <div class="table-card">
      <table class="table">
        <thead>
          <tr>
            <th>Title</th>
            <th>Preview</th>
            <th style="width:120px"></th>
          </tr>
        </thead>
        <tbody>
          @for (t of templates; track t.id) {
            <tr>
              <td style="font-weight:500">{{ t.title }}</td>
              <td style="color:var(--color-text-muted);max-width:320px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis">
                {{ t.body }}
              </td>
              <td>
                <div style="display:flex;gap:8px">
                  <button class="btn btn-secondary" style="padding:4px 12px;font-size:13px" (click)="startEdit(t)">Edit</button>
                  <button class="btn btn-danger" style="padding:4px 12px;font-size:13px" (click)="delete(t.id)">Delete</button>
                </div>
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  }
</div>
```

- [ ] **Step 3: Update `account.routes.ts`**

Replace the entire file:

```typescript
import { Routes } from '@angular/router';

export const accountRoutes: Routes = [
  {
    path: 'provision-templates',
    loadComponent: () =>
      import('./provision-templates/provision-templates.component')
        .then(m => m.ProvisionTemplatesComponent)
  }
];
```

- [ ] **Step 4: Add sidebar nav link in `app.html`**

Inside the `@if (user()?.role === 'Landlord')` block in the sidebar (after the Leases link), add:

```html
          <a routerLink="/account/provision-templates" routerLinkActive="active" class="nav-item">
            <span class="nav-icon">&#128221;</span> Clause Library
          </a>
```

The landlord nav block should now look like:

```html
        @if (user()?.role === 'Landlord') {
          <a routerLink="/properties" routerLinkActive="active" class="nav-item">
            <span class="nav-icon">&#8962;</span> Properties
          </a>
          <a routerLink="/leases" routerLinkActive="active" class="nav-item">
            <span class="nav-icon">&#128196;</span> Leases
          </a>
          <a routerLink="/account/provision-templates" routerLinkActive="active" class="nav-item">
            <span class="nav-icon">&#128221;</span> Clause Library
          </a>
        }
```

- [ ] **Step 5: Commit**

```
git add frontend/rental-management-ui/src/app/features/account/provision-templates/
git add frontend/rental-management-ui/src/app/features/account/account.routes.ts
git add frontend/rental-management-ui/src/app/app.html
git commit -m "feat(provisions): add Provision Templates management page and sidebar nav"
```

---

## Task 10: Update Lease Form with provisions picker

**Files:**
- Modify: `frontend/rental-management-ui/src/app/features/leases/lease-form/lease-form.component.ts`
- Modify: `frontend/rental-management-ui/src/app/features/leases/lease-form/lease-form.component.html`

The lease form adds provisions in two steps: (1) user selects/adds provisions in the form; (2) after the lease is created, the provisions are posted via a second API call.

- [ ] **Step 1: Update `lease-form.component.ts`**

Replace the entire file:

```typescript
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { Property } from '../../../core/models/property.model';
import { ProvisionTemplate } from '../../../core/models/provision.model';

@Component({
  selector: 'app-lease-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterLink],
  templateUrl: './lease-form.component.html'
})
export class LeaseFormComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  properties: Property[] = [];
  templates: ProvisionTemplate[] = [];
  selectedTemplateIds = new Set<string>();
  oneOffProvisions: { title: string; body: string }[] = [];
  saving = false;

  form: FormGroup = this.fb.group({
    propertyId: ['', Validators.required],
    tenantEmail: ['', [Validators.required, Validators.email]],
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
    monthlyRent: [null, [Validators.required, Validators.min(1)]],
    depositAmount: [0, [Validators.required, Validators.min(0)]],
    advanceAmount: [0, [Validators.required, Validators.min(0)]]
  });

  ngOnInit() {
    this.api.getProperties().subscribe({
      next: (props) => this.properties = props.filter(p => !p.isOccupied),
      error: () => this.toast.error('Failed to load properties.')
    });
    this.api.getProvisionTemplates().subscribe({
      next: (t) => this.templates = t,
      error: () => {}
    });
  }

  toggleTemplate(id: string) {
    if (this.selectedTemplateIds.has(id)) {
      this.selectedTemplateIds.delete(id);
    } else {
      this.selectedTemplateIds.add(id);
    }
  }

  addOneOff() {
    this.oneOffProvisions.push({ title: '', body: '' });
  }

  removeOneOff(index: number) {
    this.oneOffProvisions.splice(index, 1);
  }

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving = true;
    const v = this.form.value;

    this.api.createLease({
      propertyId: v.propertyId,
      tenantEmail: v.tenantEmail,
      startDate: v.startDate,
      endDate: v.endDate,
      monthlyRent: +v.monthlyRent,
      depositAmount: +v.depositAmount,
      advanceAmount: +v.advanceAmount
    }).subscribe({
      next: (lease) => {
        const provisions = this.buildProvisionPayloads();
        if (provisions.length === 0) {
          this.toast.success('Lease created and payments scheduled.');
          this.router.navigate(['/leases', lease.id]);
          return;
        }
        this.api.setLeaseProvisions(lease.id, provisions).subscribe({
          next: () => {
            this.toast.success('Lease created with special provisions.');
            this.router.navigate(['/leases', lease.id]);
          },
          error: () => {
            this.toast.success('Lease created. Failed to save provisions — add them from the lease detail page.');
            this.router.navigate(['/leases', lease.id]);
          }
        });
      },
      error: (err) => {
        const code = err?.error?.code;
        const msg = code === 'PROPERTY_ALREADY_OCCUPIED' ? 'Property already has an active lease.'
                  : code === 'TENANT_NOT_FOUND' ? 'No tenant account found with that email.'
                  : code === 'INVALID_DATE_RANGE' ? 'End date must be after start date.'
                  : 'Failed to create lease.';
        this.toast.error(msg);
        setTimeout(() => this.saving = false);
      }
    });
  }

  private buildProvisionPayloads() {
    const fromTemplates = this.templates
      .filter(t => this.selectedTemplateIds.has(t.id))
      .map((t, idx) => ({ title: t.title, body: t.body, sortOrder: idx }));

    const fromOneOff = this.oneOffProvisions
      .filter(p => p.title.trim() && p.body.trim())
      .map((p, idx) => ({ title: p.title.trim(), body: p.body.trim(), sortOrder: fromTemplates.length + idx }));

    return [...fromTemplates, ...fromOneOff];
  }

  hasError(field: string) {
    const ctrl = this.form.get(field);
    return ctrl?.invalid && ctrl?.touched;
  }
}
```

- [ ] **Step 2: Update `lease-form.component.html`**

Replace the entire file:

```html
<a routerLink="/leases" class="back-link">← Back to Leases</a>

<div class="page-header"><h1>New Lease</h1><p class="page-subtitle">Assign a tenant to a vacant property</p></div>

<div style="max-width:600px">
  <div class="table-card" style="padding:28px">
    <form [formGroup]="form" (ngSubmit)="submit()">
      <div class="form-group">
        <label>Property *</label>
        <select formControlName="propertyId">
          <option value="">— Select vacant property —</option>
          @for (p of properties; track p.id) {
            <option [value]="p.id">{{ p.name }} ({{ p.address }})</option>
          }
        </select>
        @if (hasError('propertyId')) { <span class="field-error">Property is required.</span> }
      </div>

      <div class="form-group">
        <label>Tenant Email *</label>
        <input formControlName="tenantEmail" type="email" placeholder="tenant@example.com" />
        <small style="color:var(--color-text-muted)">The tenant must already have a Tenant account.</small>
        @if (hasError('tenantEmail')) { <span class="field-error">Valid email is required.</span> }
      </div>

      <div class="form-row">
        <div class="form-group">
          <label>Start Date *</label>
          <input formControlName="startDate" type="date" />
          @if (hasError('startDate')) { <span class="field-error">Required.</span> }
        </div>
        <div class="form-group">
          <label>End Date *</label>
          <input formControlName="endDate" type="date" />
          @if (hasError('endDate')) { <span class="field-error">Required.</span> }
        </div>
      </div>

      <div class="form-group">
        <label>Monthly Rent (₱) *</label>
        <input formControlName="monthlyRent" type="number" min="1" placeholder="e.g. 15000" />
        @if (hasError('monthlyRent')) { <span class="field-error">Monthly rent is required.</span> }
      </div>

      <div class="form-row">
        <div class="form-group">
          <label>Deposit Amount (₱)</label>
          <input formControlName="depositAmount" type="number" min="0" />
        </div>
        <div class="form-group">
          <label>Advance Amount (₱)</label>
          <input formControlName="advanceAmount" type="number" min="0" />
        </div>
      </div>

      <!-- Special Provisions -->
      <div style="border-top:1px solid var(--color-border);padding-top:20px;margin-top:8px">
        <h3 style="font-size:14px;font-weight:600;margin-bottom:12px">Special Provisions (optional)</h3>

        @if (templates.length > 0) {
          <p style="font-size:13px;color:var(--color-text-muted);margin-bottom:8px">Select from your clause library:</p>
          @for (t of templates; track t.id) {
            <label style="display:flex;align-items:flex-start;gap:10px;margin-bottom:8px;cursor:pointer">
              <input type="checkbox"
                     [checked]="selectedTemplateIds.has(t.id)"
                     (change)="toggleTemplate(t.id)"
                     style="margin-top:3px;width:16px;height:16px" />
              <span>
                <strong>{{ t.title }}</strong>
                <span style="display:block;font-size:12px;color:var(--color-text-muted)">{{ t.body }}</span>
              </span>
            </label>
          }
        }

        @if (oneOffProvisions.length > 0) {
          <p style="font-size:13px;color:var(--color-text-muted);margin:12px 0 8px">One-off provisions:</p>
          @for (p of oneOffProvisions; track $index) {
            <div style="background:var(--color-bg-subtle,#f8f9fa);border-radius:6px;padding:12px;margin-bottom:8px">
              <div class="form-group" style="margin-bottom:8px">
                <input [(ngModel)]="p.title" [ngModelOptions]="{standalone: true}"
                       type="text" placeholder="Provision title" />
              </div>
              <div class="form-group" style="margin-bottom:8px">
                <textarea [(ngModel)]="p.body" [ngModelOptions]="{standalone: true}"
                          rows="2" placeholder="Clause text…" style="width:100%;resize:vertical"></textarea>
              </div>
              <button type="button" class="btn btn-danger" style="padding:2px 10px;font-size:12px"
                      (click)="removeOneOff($index)">Remove</button>
            </div>
          }
        }

        <button type="button" class="btn btn-secondary" style="font-size:13px;margin-top:4px"
                (click)="addOneOff()">+ Add one-off provision</button>
      </div>

      <div class="modal-actions" style="margin-top:20px">
        <a routerLink="/leases" class="btn btn-secondary">Cancel</a>
        <button type="submit" class="btn btn-primary" [disabled]="saving">
          {{ saving ? 'Creating…' : 'Create Lease' }}
        </button>
      </div>
    </form>
  </div>
</div>
```

- [ ] **Step 3: Commit**

```
git add frontend/rental-management-ui/src/app/features/leases/lease-form/
git commit -m "feat(provisions): add provisions picker to lease form"
```

---

## Task 11: Update Lease Detail with provisions card

**Files:**
- Modify: `frontend/rental-management-ui/src/app/features/leases/lease-detail/lease-detail.component.ts`
- Modify: `frontend/rental-management-ui/src/app/features/leases/lease-detail/lease-detail.component.html`

- [ ] **Step 1: Update `lease-detail.component.ts`**

Replace the entire file:

```typescript
import { Component, OnInit, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { Lease } from '../../../core/models/lease.model';
import { LeaseProvision, LeaseProvisionPayload, ProvisionTemplate } from '../../../core/models/provision.model';

@Component({
  selector: 'app-lease-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, FormsModule],
  templateUrl: './lease-detail.component.html'
})
export class LeaseDetailComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private auth = inject(AuthService);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  lease: Lease | null = null;
  loading = true;
  isLandlord = computed(() => this.auth.currentUser()?.role === 'Landlord');

  reminderLoading = false;
  savingReminder = false;

  reminderForm: FormGroup = this.fb.group({
    isEnabled: [true],
    daysBeforeDue: [3, [Validators.required, Validators.min(0), Validators.max(30)]],
    daysAfterDue: [1, [Validators.required, Validators.min(0), Validators.max(30)]]
  });

  // Provisions
  provisions: LeaseProvision[] = [];
  provisionsLoading = false;
  savingProvisions = false;
  editingProvisions = false;
  templates: ProvisionTemplate[] = [];
  draftProvisions: { id?: string; title: string; body: string; sortOrder: number }[] = [];

  get id() { return this.route.snapshot.paramMap.get('id')!; }

  ngOnInit() {
    this.api.getLease(this.id).subscribe({
      next: (l) => { this.lease = l; this.loading = false; },
      error: () => { this.toast.error('Failed to load lease.'); this.loading = false; }
    });

    if (this.auth.currentUser()?.role === 'Landlord') {
      this.loadReminderSettings();
      this.api.getProvisionTemplates().subscribe({ next: (t) => this.templates = t, error: () => {} });
    }

    this.loadProvisions();
  }

  loadProvisions() {
    this.provisionsLoading = true;
    this.api.getLeaseProvisions(this.id).subscribe({
      next: (p) => { this.provisions = p; this.provisionsLoading = false; },
      error: () => this.provisionsLoading = false
    });
  }

  startEditProvisions() {
    this.draftProvisions = this.provisions.map(p => ({
      id: p.id, title: p.title, body: p.body, sortOrder: p.sortOrder
    }));
    this.editingProvisions = true;
  }

  cancelEditProvisions() {
    this.editingProvisions = false;
    this.draftProvisions = [];
  }

  addDraftProvision() {
    this.draftProvisions.push({ title: '', body: '', sortOrder: this.draftProvisions.length });
  }

  removeDraftProvision(index: number) {
    this.draftProvisions.splice(index, 1);
    this.draftProvisions.forEach((p, i) => p.sortOrder = i);
  }

  moveDraftProvision(index: number, direction: -1 | 1) {
    const swapIdx = index + direction;
    if (swapIdx < 0 || swapIdx >= this.draftProvisions.length) return;
    [this.draftProvisions[index], this.draftProvisions[swapIdx]] =
      [this.draftProvisions[swapIdx], this.draftProvisions[index]];
    this.draftProvisions.forEach((p, i) => p.sortOrder = i);
  }

  addTemplateToProvisions(t: ProvisionTemplate) {
    const alreadyAdded = this.draftProvisions.some(p => p.title === t.title && p.body === t.body);
    if (alreadyAdded) return;
    this.draftProvisions.push({ title: t.title, body: t.body, sortOrder: this.draftProvisions.length });
  }

  saveProvisions() {
    const valid = this.draftProvisions.every(p => p.title.trim() && p.body.trim());
    if (!valid) { this.toast.error('All provisions must have a title and body.'); return; }

    this.savingProvisions = true;
    const payloads: LeaseProvisionPayload[] = this.draftProvisions.map((p, i) => ({
      title: p.title.trim(),
      body: p.body.trim(),
      sortOrder: i
    }));

    this.api.setLeaseProvisions(this.id, payloads).subscribe({
      next: (p) => {
        this.provisions = p;
        this.editingProvisions = false;
        this.draftProvisions = [];
        this.toast.success('Provisions saved.');
      },
      error: () => this.toast.error('Failed to save provisions.'),
      complete: () => setTimeout(() => this.savingProvisions = false)
    });
  }

  loadReminderSettings() {
    this.reminderLoading = true;
    this.api.getReminderSettings(this.id).subscribe({
      next: (s) => {
        this.reminderForm.patchValue({
          isEnabled: s.isEnabled,
          daysBeforeDue: s.daysBeforeDue,
          daysAfterDue: s.daysAfterDue
        });
        this.reminderLoading = false;
      },
      error: () => { this.reminderLoading = false; }
    });
  }

  saveReminders() {
    if (this.reminderForm.invalid) return;
    this.savingReminder = true;
    this.api.updateReminderSettings(this.id, this.reminderForm.value).subscribe({
      next: () => { this.toast.success('Reminder settings saved.'); },
      error: () => { this.toast.error('Failed to save reminder settings.'); },
      complete: () => setTimeout(() => this.savingReminder = false)
    });
  }

  statusClass(s: string) {
    return s === 'Active' ? 'badge-green' : s === 'Terminated' ? 'badge-red' : 'badge-yellow';
  }

  formatDate(d: string) {
    return new Date(d).toLocaleDateString('en-PH', { year: 'numeric', month: 'long', day: 'numeric' });
  }

  formatMoney(n: number) {
    return '₱' + n.toLocaleString('en-PH', { minimumFractionDigits: 2 });
  }
}
```

- [ ] **Step 2: Update `lease-detail.component.html`**

Replace the entire file:

```html
<a routerLink="/leases" class="back-link">← Back to Leases</a>

@if (loading) {
  <div class="loading">Loading…</div>
} @else if (lease) {
  <div class="page-header">
    <div style="display:flex;align-items:center;justify-content:space-between">
      <div>
        <h1>{{ lease.propertyName }}</h1>
        <p class="page-subtitle">{{ lease.propertyAddress }}</p>
      </div>
      <span class="badge {{ statusClass(lease.status) }}" style="font-size:14px;padding:4px 14px">{{ lease.status }}</span>
    </div>
  </div>

  <div class="detail-grid">
    <div class="detail-item">
      <label>Tenant</label>
      <p>{{ lease.tenantName }}</p>
    </div>
    <div class="detail-item">
      <label>Tenant Email</label>
      <p>{{ lease.tenantEmail }}</p>
    </div>
    <div class="detail-item">
      <label>Start Date</label>
      <p>{{ formatDate(lease.startDate) }}</p>
    </div>
    <div class="detail-item">
      <label>End Date</label>
      <p>{{ formatDate(lease.endDate) }}</p>
    </div>
    <div class="detail-item">
      <label>Monthly Rent</label>
      <p>{{ formatMoney(lease.monthlyRent) }}</p>
    </div>
    <div class="detail-item">
      <label>Deposit</label>
      <p>{{ formatMoney(lease.depositAmount) }}</p>
    </div>
    <div class="detail-item">
      <label>Advance</label>
      <p>{{ formatMoney(lease.advanceAmount) }}</p>
    </div>
    @if (lease.tenantIdFileUrl) {
      <div class="detail-item">
        <label>Tenant ID</label>
        <p><a [href]="lease.tenantIdFileUrl" target="_blank" style="color:var(--color-primary)">View document</a></p>
      </div>
    }
  </div>

  <div style="display:flex;gap:12px;margin-bottom:32px">
    <a routerLink="/payments" class="btn btn-secondary">View Payments</a>
    <a [routerLink]="['/leases', id, 'pdf-preview']" class="btn btn-secondary">View Lease PDF</a>
  </div>

  <!-- Special Provisions card -->
  <div class="form-card" style="max-width:600px;margin-bottom:32px">
    <div class="form-card-header" style="display:flex;align-items:center;justify-content:space-between">
      <div>
        <h2 style="font-size:16px;font-weight:600">Special Provisions</h2>
        <p class="page-subtitle">Custom clauses included in the lease PDF</p>
      </div>
      @if (isLandlord() && !editingProvisions) {
        <button class="btn btn-secondary" style="font-size:13px" (click)="startEditProvisions()">Edit</button>
      }
    </div>

    @if (provisionsLoading) {
      <div class="loading" style="padding:12px 0">Loading…</div>
    } @else if (!editingProvisions) {
      @if (provisions.length === 0) {
        <p style="color:var(--color-text-muted);font-size:14px;padding:8px 0">No special provisions on this lease.</p>
      } @else {
        @for (p of provisions; track p.id) {
          <div style="border-top:1px solid var(--color-border);padding:12px 0">
            <p style="font-weight:600;margin-bottom:4px">{{ p.title }}</p>
            <p style="font-size:13px;color:var(--color-text-muted);white-space:pre-wrap">{{ p.body }}</p>
          </div>
        }
      }
    } @else {
      <!-- Edit mode (landlord only) -->
      @if (templates.length > 0) {
        <div style="margin-bottom:16px">
          <p style="font-size:13px;font-weight:500;margin-bottom:8px">Add from clause library:</p>
          @for (t of templates; track t.id) {
            <button type="button" class="btn btn-secondary"
                    style="font-size:12px;padding:3px 10px;margin:0 6px 6px 0"
                    (click)="addTemplateToProvisions(t)">
              + {{ t.title }}
            </button>
          }
        </div>
      }

      @for (p of draftProvisions; track $index) {
        <div style="background:var(--color-bg-subtle,#f8f9fa);border-radius:6px;padding:12px;margin-bottom:8px">
          <div class="form-group" style="margin-bottom:8px">
            <input [(ngModel)]="p.title" type="text" placeholder="Provision title" />
          </div>
          <div class="form-group" style="margin-bottom:8px">
            <textarea [(ngModel)]="p.body" rows="3" placeholder="Clause text…" style="width:100%;resize:vertical"></textarea>
          </div>
          <div style="display:flex;gap:6px">
            <button type="button" class="btn btn-secondary" style="padding:2px 8px;font-size:12px"
                    [disabled]="$index === 0" (click)="moveDraftProvision($index, -1)">↑</button>
            <button type="button" class="btn btn-secondary" style="padding:2px 8px;font-size:12px"
                    [disabled]="$index === draftProvisions.length - 1" (click)="moveDraftProvision($index, 1)">↓</button>
            <button type="button" class="btn btn-danger" style="padding:2px 8px;font-size:12px"
                    (click)="removeDraftProvision($index)">Remove</button>
          </div>
        </div>
      }

      <button type="button" class="btn btn-secondary" style="font-size:13px;margin-top:4px"
              (click)="addDraftProvision()">+ Add provision</button>

      <div style="display:flex;gap:12px;margin-top:16px">
        <button class="btn btn-primary" [disabled]="savingProvisions" (click)="saveProvisions()">
          {{ savingProvisions ? 'Saving…' : 'Save Provisions' }}
        </button>
        <button class="btn btn-secondary" (click)="cancelEditProvisions()">Cancel</button>
      </div>
    }
  </div>

  @if (isLandlord()) {
    <div class="form-card" style="max-width:480px">
      <div class="form-card-header">
        <h2 style="font-size:16px;font-weight:600">Rent Reminders</h2>
        <p class="page-subtitle">Email reminders sent to the tenant automatically</p>
      </div>
      @if (reminderLoading) {
        <div class="loading" style="padding:12px 0">Loading settings…</div>
      } @else {
        <form [formGroup]="reminderForm" (ngSubmit)="saveReminders()">
          <div class="form-group" style="display:flex;align-items:center;gap:12px;margin-bottom:16px">
            <input type="checkbox" formControlName="isEnabled" id="isEnabled" style="width:18px;height:18px" />
            <label for="isEnabled" style="margin:0;font-weight:500">Enable reminders for this lease</label>
          </div>
          <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px">
            <div class="form-group">
              <label>Days before due date</label>
              <input type="number" formControlName="daysBeforeDue" min="0" max="30" />
            </div>
            <div class="form-group">
              <label>Days after due date</label>
              <input type="number" formControlName="daysAfterDue" min="0" max="30" />
            </div>
          </div>
          <button type="submit" class="btn btn-primary" [disabled]="savingReminder || reminderForm.invalid">
            {{ savingReminder ? 'Saving…' : 'Save Reminder Settings' }}
          </button>
        </form>
      }
    </div>
  }
}
```

- [ ] **Step 3: Commit**

```
git add frontend/rental-management-ui/src/app/features/leases/lease-detail/
git commit -m "feat(provisions): add provisions card to lease detail"
```

---

## Task 12: Final verification

- [ ] **Step 1: Build the backend**

Run from `backend/RentalManagementApi/`:

```
dotnet build
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 2: Build the frontend**

Run from `frontend/rental-management-ui/`:

```
ng build
```

Expected: `Application bundle generation complete.`

- [ ] **Step 3: Smoke-test the happy path (manual)**

With both backend and frontend running:

1. Log in as a landlord
2. Navigate to **Clause Library** in the sidebar
3. Create two templates: e.g. "No Pets Policy" and "Early Termination Fee"
4. Navigate to **Leases > New Lease** — verify the two templates appear in the Special Provisions section
5. Check one template, add one one-off provision, create the lease
6. Open the lease detail — verify the provisions card shows the two provisions
7. Click **Edit** in the provisions card, add another provision, save
8. Click **View Lease PDF** — verify the Special Provisions section appears before the signature block with bold titles and body text
9. Log in as the tenant for that lease
10. Open the lease detail — verify the provisions card is read-only (no Edit button)
11. Open the lease PDF — verify the same Special Provisions section is visible

- [ ] **Step 4: Final commit**

```
git add .
git commit -m "feat(wave-5): lease special provisions — clause library, per-lease provisions, PDF rendering"
```
