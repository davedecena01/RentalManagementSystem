# Wave 4 — PDF Generation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generate on-demand PDF lease agreements and payment receipts, viewable in dedicated Angular preview pages with a download button.

**Architecture:** Backend `PdfService` uses QuestPDF (already installed, MIT license) to generate PDFs from DB data. Two new controller endpoints stream `application/pdf`. Angular fetches the endpoint as a Blob, creates an object URL, and renders it in an `<iframe>` on a dedicated preview page.

**Tech Stack:** QuestPDF 2026.2.4 (already in .csproj), .NET 10 C#, Angular 18 standalone components, DomSanitizer.

---

### Task 1: Configure QuestPDF License + Register PdfService

**Files:**
- Modify: `backend/RentalManagementApi/Program.cs`

QuestPDF requires a one-time license declaration before any PDF is generated. Community license is free and open-source.

- [ ] **Step 1: Add license declaration and service registration to Program.cs**

Open `backend/RentalManagementApi/Program.cs`. Add after the `using` statements at the top (before `var builder = ...`):

```csharp
using QuestPDF.Infrastructure;
```

Then add this line immediately before `var builder = WebApplication.CreateBuilder(args);`:

```csharp
QuestPDF.Settings.License = LicenseType.Community;
```

Then in the Services section (after `builder.Services.AddScoped<ReminderService>();`), add:

```csharp
builder.Services.AddScoped<PdfService>();
```

- [ ] **Step 2: Verify the project builds**

```powershell
cd backend/RentalManagementApi
dotnet build
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```powershell
git add backend/RentalManagementApi/Program.cs
git commit -m "feat(pdf): configure QuestPDF license and register PdfService"
```

---

### Task 2: Create PdfService — Lease Agreement Generator

**Files:**
- Create: `backend/RentalManagementApi/Application/Services/PdfService.cs`

- [ ] **Step 1: Create PdfService.cs with the lease PDF method**

Create `backend/RentalManagementApi/Application/Services/PdfService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RentalManagementApi.Data;

namespace RentalManagementApi.Application.Services;

public class PdfService(AppDbContext db)
{
    public async Task<byte[]> GenerateLeasePdfAsync(Guid leaseId, Guid requestorId, string requestorRole)
    {
        var lease = await db.Leases
            .Include(l => l.Property).ThenInclude(p => p.Landlord)
            .Include(l => l.Tenant)
            .FirstOrDefaultAsync(l => l.Id == leaseId);

        if (lease is null) throw new KeyNotFoundException("Lease not found.");

        var hasAccess = requestorRole == "Landlord"
            ? lease.Property.LandlordId == requestorId
            : lease.TenantId == requestorId;

        if (!hasAccess) throw new UnauthorizedAccessException("Access denied.");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("Rental Property Manager")
                        .SemiBold().FontSize(14).FontColor(Colors.Grey.Darken2);
                    col.Item().Text("LEASE AGREEMENT")
                        .Bold().FontSize(22).FontColor(Colors.Black);
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    col.Spacing(16);

                    col.Item().Text("Property").Bold().FontSize(13).FontColor(Colors.Grey.Darken2);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.ConstantColumn(140); c.RelativeColumn(); });
                        AddRow(t, "Name", lease.Property.Name);
                        AddRow(t, "Address", lease.Property.Address);
                        AddRow(t, "Type", lease.Property.Type.ToString());
                    });

                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                    col.Item().Text("Parties").Bold().FontSize(13).FontColor(Colors.Grey.Darken2);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.ConstantColumn(140); c.RelativeColumn(); });
                        AddRow(t, "Landlord", $"{lease.Property.Landlord.FirstName} {lease.Property.Landlord.LastName}");
                        AddRow(t, "Tenant", $"{lease.Tenant.FirstName} {lease.Tenant.LastName}");
                        AddRow(t, "Tenant Email", lease.Tenant.Email);
                    });

                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                    col.Item().Text("Lease Terms").Bold().FontSize(13).FontColor(Colors.Grey.Darken2);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.ConstantColumn(140); c.RelativeColumn(); });
                        AddRow(t, "Start Date", lease.StartDate.ToString("MMMM dd, yyyy"));
                        AddRow(t, "End Date", lease.EndDate.ToString("MMMM dd, yyyy"));
                        AddRow(t, "Monthly Rent", $"₱{lease.MonthlyRent:N2}");
                        AddRow(t, "Security Deposit", $"₱{lease.DepositAmount:N2}");
                        AddRow(t, "Advance Payment", $"₱{lease.AdvanceAmount:N2}");
                        AddRow(t, "Status", lease.Status.ToString());
                    });

                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                    col.Item().Text("Signatures").Bold().FontSize(13).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().PaddingBottom(40).Text("Landlord Signature");
                            c.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(4).Text($"{lease.Property.Landlord.FirstName} {lease.Property.Landlord.LastName}")
                                .FontSize(9).FontColor(Colors.Grey.Darken2);
                            c.Item().Text("Date: _______________").FontSize(9).FontColor(Colors.Grey.Darken2);
                        });
                        row.ConstantItem(40);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().PaddingBottom(40).Text("Tenant Signature");
                            c.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(4).Text($"{lease.Tenant.FirstName} {lease.Tenant.LastName}")
                                .FontSize(9).FontColor(Colors.Grey.Darken2);
                            c.Item().Text("Date: _______________").FontSize(9).FontColor(Colors.Grey.Darken2);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("This document was generated electronically by Rental Property Manager on ");
                    text.Span(DateTime.UtcNow.ToString("MMMM dd, yyyy")).SemiBold();
                });
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> GeneratePaymentReceiptPdfAsync(Guid paymentId, Guid requestorId, string requestorRole)
    {
        var payment = await db.Payments
            .Include(p => p.Lease).ThenInclude(l => l.Property).ThenInclude(p => p.Landlord)
            .Include(p => p.Lease).ThenInclude(l => l.Tenant)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment is null) throw new KeyNotFoundException("Payment not found.");

        var hasAccess = requestorRole == "Landlord"
            ? payment.Lease.Property.LandlordId == requestorId
            : payment.Lease.TenantId == requestorId;

        if (!hasAccess) throw new UnauthorizedAccessException("Access denied.");

        var receiptRef = payment.Id.ToString()[..8].ToUpper();
        var balance = payment.AmountDue - payment.AmountPaid;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("Rental Property Manager")
                        .SemiBold().FontSize(14).FontColor(Colors.Grey.Darken2);
                    col.Item().Text("PAYMENT RECEIPT")
                        .Bold().FontSize(22).FontColor(Colors.Black);
                    col.Item().Text($"Receipt #{receiptRef}")
                        .FontSize(11).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    col.Spacing(16);

                    col.Item().Text("Property").Bold().FontSize(13).FontColor(Colors.Grey.Darken2);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.ConstantColumn(140); c.RelativeColumn(); });
                        AddRow(t, "Name", payment.Lease.Property.Name);
                        AddRow(t, "Address", payment.Lease.Property.Address);
                    });

                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                    col.Item().Text("Tenant").Bold().FontSize(13).FontColor(Colors.Grey.Darken2);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.ConstantColumn(140); c.RelativeColumn(); });
                        AddRow(t, "Name", $"{payment.Lease.Tenant.FirstName} {payment.Lease.Tenant.LastName}");
                        AddRow(t, "Email", payment.Lease.Tenant.Email);
                    });

                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                    col.Item().Text("Payment Details").Bold().FontSize(13).FontColor(Colors.Grey.Darken2);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.ConstantColumn(140); c.RelativeColumn(); });
                        AddRow(t, "Due Date", payment.DueDate.ToString("MMMM dd, yyyy"));
                        AddRow(t, "Amount Due", $"₱{payment.AmountDue:N2}");
                        AddRow(t, "Amount Paid", $"₱{payment.AmountPaid:N2}");
                        AddRow(t, "Balance", $"₱{balance:N2}");
                        AddRow(t, "Status", payment.Status.ToString());
                        if (payment.PaidAt.HasValue)
                            AddRow(t, "Date Paid", payment.PaidAt.Value.ToString("MMMM dd, yyyy"));
                        if (!string.IsNullOrEmpty(payment.Notes))
                            AddRow(t, "Notes", payment.Notes);
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated by Rental Property Manager on ");
                    text.Span(DateTime.UtcNow.ToString("MMMM dd, yyyy")).SemiBold();
                });
            });
        }).GeneratePdf();
    }

    private static void AddRow(TableDescriptor t, string label, string value)
    {
        t.Cell().PaddingVertical(4).Text(label).SemiBold().FontColor(Colors.Grey.Darken2);
        t.Cell().PaddingVertical(4).Text(value);
    }
}
```

- [ ] **Step 2: Build to verify no compile errors**

```powershell
cd backend/RentalManagementApi
dotnet build
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```powershell
git add backend/RentalManagementApi/Application/Services/PdfService.cs
git commit -m "feat(pdf): add PdfService with lease agreement and payment receipt generators"
```

---

### Task 3: Add PDF Endpoints to Controllers

**Files:**
- Modify: `backend/RentalManagementApi/Controllers/LeasesController.cs`
- Modify: `backend/RentalManagementApi/Controllers/PaymentsController.cs`

- [ ] **Step 1: Update LeasesController primary constructor and add GET pdf endpoint**

The current constructor is `LeasesController(LeaseService leaseService)`. Replace with:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalManagementApi.Application.Services;
using RentalManagementApi.Common;
using RentalManagementApi.DTOs.Leases;

namespace RentalManagementApi.Controllers;

[ApiController]
[Route("api/leases")]
[Authorize]
public class LeasesController(LeaseService leaseService, PdfService pdfService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (userId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var leases = role == "Landlord"
            ? await leaseService.GetAllForLandlordAsync(userId.Value)
            : await leaseService.GetAllForTenantAsync(userId.Value);

        return Ok(leases);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (userId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var lease = role == "Landlord"
            ? await leaseService.GetByIdForLandlordAsync(id, userId.Value)
            : await leaseService.GetByIdForTenantAsync(id, userId.Value);

        if (lease is null) return NotFound(new ApiError("Lease not found.", "LEASE_NOT_FOUND"));
        return Ok(lease);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GetPdf(Guid id)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        if (userId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        try
        {
            var bytes = await pdfService.GenerateLeasePdfAsync(id, userId.Value, role);
            Response.Headers.Append("Content-Disposition", "inline; filename=\"lease-agreement.pdf\"");
            return File(bytes, "application/pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiError("Lease not found.", "LEASE_NOT_FOUND"));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    [Authorize(Policy = "LandlordPolicy")]
    public async Task<IActionResult> Create([FromBody] CreateLeaseRequest request)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var (lease, error) = await leaseService.CreateAsync(landlordId.Value, request);

        return error switch
        {
            "PROPERTY_NOT_FOUND" => NotFound(new ApiError("Property not found.", error)),
            "PROPERTY_ALREADY_OCCUPIED" => Conflict(new ApiError("Property already has an active lease.", error)),
            "INVALID_DATE_RANGE" => BadRequest(new ApiError("End date must be after start date.", error)),
            "TENANT_NOT_FOUND" => NotFound(new ApiError("No tenant found with that email.", error)),
            not null => BadRequest(new ApiError("Could not create lease.", error)),
            _ => CreatedAtAction(nameof(GetById), new { id = lease!.Id }, lease)
        };
    }

    [HttpPatch("{id:guid}/terminate")]
    [Authorize(Policy = "LandlordPolicy")]
    public async Task<IActionResult> Terminate(Guid id)
    {
        var landlordId = GetCurrentUserId();
        if (landlordId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

        var lease = await leaseService.TerminateAsync(id, landlordId.Value);
        if (lease is null) return NotFound(new ApiError("Lease not found.", "LEASE_NOT_FOUND"));

        return Ok(lease);
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

- [ ] **Step 2: Update PaymentsController primary constructor and add GET receipt endpoint**

Open `backend/RentalManagementApi/Controllers/PaymentsController.cs`. Change the constructor:

```csharp
public class PaymentsController(
    PaymentService paymentService,
    PdfService pdfService,
    IOptions<StripeOptions> stripeOptions,
    IOptions<AppOptions> appOptions) : ControllerBase
```

Add this action after `GetById`:

```csharp
[HttpGet("{id:guid}/receipt")]
public async Task<IActionResult> GetReceipt(Guid id)
{
    var userId = GetCurrentUserId();
    var role = GetCurrentUserRole();
    if (userId is null) return Unauthorized(new ApiError("Invalid token.", "UNAUTHORIZED"));

    try
    {
        var bytes = await pdfService.GeneratePaymentReceiptPdfAsync(id, userId.Value, role);
        Response.Headers.Append("Content-Disposition", "inline; filename=\"payment-receipt.pdf\"");
        return File(bytes, "application/pdf");
    }
    catch (KeyNotFoundException)
    {
        return NotFound(new ApiError("Payment not found.", "PAYMENT_NOT_FOUND"));
    }
    catch (UnauthorizedAccessException)
    {
        return Forbid();
    }
}
```

- [ ] **Step 3: Build to verify**

```powershell
cd backend/RentalManagementApi
dotnet build
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```powershell
git add backend/RentalManagementApi/Controllers/LeasesController.cs
git add backend/RentalManagementApi/Controllers/PaymentsController.cs
git commit -m "feat(pdf): add GET /api/leases/{id}/pdf and GET /api/payments/{id}/receipt endpoints"
```

---

### Task 4: Add ApiService Methods (Frontend)

**Files:**
- Modify: `frontend/rental-management-ui/src/app/core/services/api.service.ts`

- [ ] **Step 1: Add getLeasePdf and getPaymentReceipt methods to ApiService**

In `api.service.ts`, add these two methods after `terminateLease`:

```typescript
getLeasePdf(id: string): Observable<Blob> {
  return this.http.get(`${this.base}/leases/${id}/pdf`, { responseType: 'blob' });
}
```

And after `manualPay`:

```typescript
getPaymentReceipt(id: string): Observable<Blob> {
  return this.http.get(`${this.base}/payments/${id}/receipt`, { responseType: 'blob' });
}
```

Also add `Observable` to the import if not already present:

```typescript
import { Observable } from 'rxjs';
```

- [ ] **Step 2: Verify TypeScript compiles**

```powershell
cd frontend/rental-management-ui
npx tsc --noEmit
```

Expected: No errors.

- [ ] **Step 3: Commit**

```powershell
git add frontend/rental-management-ui/src/app/core/services/api.service.ts
git commit -m "feat(pdf): add getLeasePdf and getPaymentReceipt to ApiService"
```

---

### Task 5: Build LeasePdfPreviewComponent

**Files:**
- Create: `frontend/rental-management-ui/src/app/features/leases/lease-pdf-preview/lease-pdf-preview.component.ts`
- Create: `frontend/rental-management-ui/src/app/features/leases/lease-pdf-preview/lease-pdf-preview.component.html`
- Modify: `frontend/rental-management-ui/src/app/features/leases/leases.routes.ts`

- [ ] **Step 1: Create the component TypeScript file**

Create `frontend/rental-management-ui/src/app/features/leases/lease-pdf-preview/lease-pdf-preview.component.ts`:

```typescript
import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-lease-pdf-preview',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './lease-pdf-preview.component.html'
})
export class LeasePdfPreviewComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private sanitizer = inject(DomSanitizer);
  private route = inject(ActivatedRoute);

  loading = true;
  error: string | null = null;
  safeUrl: SafeResourceUrl | null = null;
  private objectUrl: string | null = null;

  get id() { return this.route.snapshot.paramMap.get('id')!; }

  ngOnInit() {
    this.api.getLeasePdf(this.id).subscribe({
      next: (blob) => {
        this.objectUrl = URL.createObjectURL(blob);
        this.safeUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.objectUrl);
        this.loading = false;
      },
      error: (err) => {
        this.error = err.status === 404
          ? 'Document not found.'
          : err.status === 403
          ? "You don't have access to this document."
          : 'Failed to generate document. Please try again.';
        this.loading = false;
      }
    });
  }

  ngOnDestroy() {
    if (this.objectUrl) URL.revokeObjectURL(this.objectUrl);
  }

  download() {
    if (!this.objectUrl) return;
    const a = document.createElement('a');
    a.href = this.objectUrl;
    a.download = 'lease-agreement.pdf';
    a.click();
  }
}
```

- [ ] **Step 2: Create the component HTML template**

Create `frontend/rental-management-ui/src/app/features/leases/lease-pdf-preview/lease-pdf-preview.component.html`:

```html
<a [routerLink]="['/leases', id]" class="back-link">← Back to Lease</a>

<div class="page-header">
  <h1>Lease Agreement</h1>
  <p class="page-subtitle">Preview and download the lease agreement document</p>
</div>

@if (loading) {
  <div class="loading">Generating PDF…</div>
} @else if (error) {
  <div class="empty-state" style="color:var(--color-danger)">{{ error }}</div>
} @else {
  <div style="margin-bottom:16px">
    <button class="btn btn-primary" (click)="download()">Download PDF</button>
  </div>
  <iframe
    [src]="safeUrl"
    style="width:100%;height:80vh;border:1px solid #e5e7eb;border-radius:8px;display:block"
    title="Lease Agreement PDF">
  </iframe>
}
```

- [ ] **Step 3: Add the pdf-preview route to leases.routes.ts**

Replace the contents of `frontend/rental-management-ui/src/app/features/leases/leases.routes.ts`:

```typescript
import { Routes } from '@angular/router';

export const leasesRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./leases-list/leases-list.component').then(m => m.LeasesListComponent)
  },
  {
    path: 'new',
    loadComponent: () => import('./lease-form/lease-form.component').then(m => m.LeaseFormComponent)
  },
  {
    path: ':id/pdf-preview',
    loadComponent: () => import('./lease-pdf-preview/lease-pdf-preview.component').then(m => m.LeasePdfPreviewComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./lease-detail/lease-detail.component').then(m => m.LeaseDetailComponent)
  }
];
```

Note: `:id/pdf-preview` must come **before** `:id` so the router doesn't partially match.

- [ ] **Step 4: Verify TypeScript compiles**

```powershell
cd frontend/rental-management-ui
npx tsc --noEmit
```

Expected: No errors.

- [ ] **Step 5: Commit**

```powershell
git add frontend/rental-management-ui/src/app/features/leases/lease-pdf-preview/
git add frontend/rental-management-ui/src/app/features/leases/leases.routes.ts
git commit -m "feat(pdf): add LeasePdfPreviewComponent and route"
```

---

### Task 6: Build ReceiptPreviewComponent

**Files:**
- Create: `frontend/rental-management-ui/src/app/features/payments/receipt-preview/receipt-preview.component.ts`
- Create: `frontend/rental-management-ui/src/app/features/payments/receipt-preview/receipt-preview.component.html`
- Modify: `frontend/rental-management-ui/src/app/features/payments/payments.routes.ts`

- [ ] **Step 1: Create the component TypeScript file**

Create `frontend/rental-management-ui/src/app/features/payments/receipt-preview/receipt-preview.component.ts`:

```typescript
import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-receipt-preview',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './receipt-preview.component.html'
})
export class ReceiptPreviewComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private sanitizer = inject(DomSanitizer);
  private route = inject(ActivatedRoute);

  loading = true;
  error: string | null = null;
  safeUrl: SafeResourceUrl | null = null;
  private objectUrl: string | null = null;

  get id() { return this.route.snapshot.paramMap.get('id')!; }

  ngOnInit() {
    this.api.getPaymentReceipt(this.id).subscribe({
      next: (blob) => {
        this.objectUrl = URL.createObjectURL(blob);
        this.safeUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.objectUrl);
        this.loading = false;
      },
      error: (err) => {
        this.error = err.status === 404
          ? 'Receipt not found.'
          : err.status === 403
          ? "You don't have access to this receipt."
          : 'Failed to generate receipt. Please try again.';
        this.loading = false;
      }
    });
  }

  ngOnDestroy() {
    if (this.objectUrl) URL.revokeObjectURL(this.objectUrl);
  }

  download() {
    if (!this.objectUrl) return;
    const a = document.createElement('a');
    a.href = this.objectUrl;
    a.download = 'payment-receipt.pdf';
    a.click();
  }
}
```

- [ ] **Step 2: Create the component HTML template**

Create `frontend/rental-management-ui/src/app/features/payments/receipt-preview/receipt-preview.component.html`:

```html
<a routerLink="/payments" class="back-link">← Back to Payments</a>

<div class="page-header">
  <h1>Payment Receipt</h1>
  <p class="page-subtitle">Preview and download the payment receipt document</p>
</div>

@if (loading) {
  <div class="loading">Generating receipt…</div>
} @else if (error) {
  <div class="empty-state" style="color:var(--color-danger)">{{ error }}</div>
} @else {
  <div style="margin-bottom:16px">
    <button class="btn btn-primary" (click)="download()">Download Receipt</button>
  </div>
  <iframe
    [src]="safeUrl"
    style="width:100%;height:80vh;border:1px solid #e5e7eb;border-radius:8px;display:block"
    title="Payment Receipt PDF">
  </iframe>
}
```

- [ ] **Step 3: Add the receipt-preview route to payments.routes.ts**

Replace the contents of `frontend/rental-management-ui/src/app/features/payments/payments.routes.ts`:

```typescript
import { Routes } from '@angular/router';

export const paymentsRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./payments-list/payments-list.component').then(m => m.PaymentsListComponent)
  },
  {
    path: ':id/receipt-preview',
    loadComponent: () => import('./receipt-preview/receipt-preview.component').then(m => m.ReceiptPreviewComponent)
  }
];
```

- [ ] **Step 4: Verify TypeScript compiles**

```powershell
cd frontend/rental-management-ui
npx tsc --noEmit
```

Expected: No errors.

- [ ] **Step 5: Commit**

```powershell
git add frontend/rental-management-ui/src/app/features/payments/receipt-preview/
git add frontend/rental-management-ui/src/app/features/payments/payments.routes.ts
git commit -m "feat(pdf): add ReceiptPreviewComponent and route"
```

---

### Task 7: Wire Up Trigger Buttons in Existing Templates

**Files:**
- Modify: `frontend/rental-management-ui/src/app/features/leases/lease-detail/lease-detail.component.html`
- Modify: `frontend/rental-management-ui/src/app/features/payments/payments-list/payments-list.component.html`
- Modify: `frontend/rental-management-ui/src/app/features/payments/payments-list/payments-list.component.ts` (add RouterLink import if missing)

- [ ] **Step 1: Add "View Lease PDF" button to lease-detail template**

In `lease-detail.component.html`, find the buttons `div` (currently line 53-55):

```html
  <div style="display:flex;gap:12px;margin-bottom:32px">
    <a routerLink="/payments" class="btn btn-secondary">View Payments</a>
  </div>
```

Replace with:

```html
  <div style="display:flex;gap:12px;margin-bottom:32px">
    <a routerLink="/payments" class="btn btn-secondary">View Payments</a>
    <a [routerLink]="['/leases', id, 'pdf-preview']" class="btn btn-secondary">View Lease PDF</a>
  </div>
```

- [ ] **Step 2: Add "View Receipt" button to payments-list template**

In `payments-list.component.html`, find the Actions `<td>` block (lines 42-57). Replace the entire `<td>` block with:

```html
            <td>
              @if (p.status !== 'Paid') {
                <div class="table-actions">
                  @if (!isLandlord()) {
                    <button class="btn btn-primary btn-sm" (click)="payWithStripe(p)">Pay Online</button>
                  }
                  <button class="btn btn-secondary btn-sm" (click)="openManualPay(p)">Manual Pay</button>
                  @if (p.status === 'Partial') {
                    <a [routerLink]="['/payments', p.id, 'receipt-preview']" class="btn btn-secondary btn-sm">View Receipt</a>
                  }
                </div>
              } @else {
                <div class="table-actions">
                  @if (p.proofFileUrl) {
                    <a [href]="p.proofFileUrl" target="_blank" class="btn btn-secondary btn-sm">Proof</a>
                  }
                  <a [routerLink]="['/payments', p.id, 'receipt-preview']" class="btn btn-secondary btn-sm">View Receipt</a>
                </div>
              }
            </td>
```

- [ ] **Step 3: Ensure RouterLink is imported in PaymentsListComponent**

Open `payments-list.component.ts` and verify `RouterLink` is in the `imports` array of the `@Component` decorator. If not, add it:

```typescript
import { RouterLink } from '@angular/router';

@Component({
  ...
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  ...
})
```

- [ ] **Step 4: Verify TypeScript compiles**

```powershell
cd frontend/rental-management-ui
npx tsc --noEmit
```

Expected: No errors.

- [ ] **Step 5: Smoke test in browser**

Start the backend and frontend. Log in as landlord:
1. Go to `/leases` → click a lease → verify "View Lease PDF" button appears
2. Click "View Lease PDF" → verify route navigates to `/leases/:id/pdf-preview`
3. Verify iframe renders the PDF with property/party/terms/signature sections
4. Click "Download PDF" → verify browser downloads `lease-agreement.pdf`
5. Go to `/payments` → find a Paid or Partial payment
6. Click "View Receipt" → verify route navigates to `/payments/:id/receipt-preview`
7. Verify iframe renders the receipt with payment details
8. Click "Download Receipt" → verify browser downloads `payment-receipt.pdf`

- [ ] **Step 6: Commit**

```powershell
git add frontend/rental-management-ui/src/app/features/leases/lease-detail/lease-detail.component.html
git add frontend/rental-management-ui/src/app/features/payments/payments-list/payments-list.component.html
git add frontend/rental-management-ui/src/app/features/payments/payments-list/payments-list.component.ts
git commit -m "feat(pdf): wire up View Lease PDF and View Receipt trigger buttons"
```

---

## Self-Review

**Spec coverage:**
- ✅ `GET /api/leases/{id}/pdf` endpoint
- ✅ `GET /api/payments/{id}/receipt` endpoint
- ✅ Authorization: landlord-owner or assigned tenant for both
- ✅ Lease PDF content: property, parties, terms, signature placeholders
- ✅ Receipt PDF content: receipt ref, property, tenant, payment details, date paid, notes
- ✅ `Content-Disposition: inline` for preview
- ✅ `/leases/:id/pdf-preview` route + LeasePdfPreviewComponent
- ✅ `/payments/:id/receipt-preview` route + ReceiptPreviewComponent
- ✅ iframe with `SafeResourceUrl` via DomSanitizer
- ✅ Loading and error states on preview pages
- ✅ `URL.revokeObjectURL` in `ngOnDestroy`
- ✅ Download button using programmatic `<a download>`
- ✅ "View Lease PDF" button on lease detail
- ✅ "View Receipt" on Paid/Partial payment rows
- ✅ QuestPDF license set in Program.cs
- ✅ PdfService registered in DI

**Type consistency:** `GenerateLeasePdfAsync` / `GeneratePaymentReceiptPdfAsync` are used consistently across Task 2 (service definition) and Task 3 (controller calls).
