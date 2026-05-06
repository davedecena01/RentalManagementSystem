# Wave 2 Handover — Rental Property Manager

**Date:** 2026-05-06
**Prepared by:** Claude (session handover)

---

## Current State

**Branch:** `feature/wave-1-foundation`
**Last commit:** `fab6eac fix(auth): resolve all Wave 1 E2E auth failures`

Wave 1 is **fully complete and E2E verified** via Playwright. The following flows work end-to-end:

- Landlord registers → redirected to login
- Landlord logs in → dashboard with role-aware nav (Properties, Leases visible)
- Topbar shows name + "Landlord" badge
- Sign out → `/auth/login`
- Auth guard blocks unauthenticated access to `/dashboard`

---

## Tech Stack (locked)

| Layer | Choice |
|---|---|
| Backend | C# .NET 10, EF Core 9, PostgreSQL |
| Frontend | Angular (standalone components), TypeScript, SCSS |
| Auth | Supabase Auth — ES256 JWT via OIDC JWKS discovery |
| Database | Supabase PostgreSQL (Session Pooler) |
| Storage | Supabase Storage |
| Payments | Stripe Checkout |
| Email | SendGrid |
| Deploy | Railway (backend), Vercel (frontend) |
| Currency | PHP (₱) — hard-coded |

---

## Key Auth Architecture — Do Not Regress

- JWT validation uses **OIDC JWKS discovery** (`MetadataAddress` in `Program.cs`), not a symmetric secret. New Supabase projects sign tokens with ES256 (asymmetric ECDSA).
- `MapInboundClaims = false` is set — controllers use `User.FindFirst("sub")` (not `ClaimTypes.NameIdentifier`).
- `POST /api/auth/register` is `[AllowAnonymous]` — Supabase user ID is passed in the request body because email confirmation means no session exists at signup time.
- `setTimeout(() => this.loading = false)` is used in all 4 Angular auth components to avoid NG0100 (ExpressionChangedAfterItHasBeenCheckedError).
- `DATABASE_URL` must use the **Session Pooler** URI (`aws-1-ap-northeast-1.pooler.supabase.com:5432`) — the direct DB host is IPv6-only and unreachable from most networks.

---

## Wave 2 Scope

**Goal:** Core landlord workflow — add a property, assign a tenant via lease, payments auto-generated, Stripe and manual payment paths both work.

### Backend Entities to Create

| Entity | Key Fields |
|---|---|
| `Property` | `Id (Guid)`, `LandlordId (Guid FK→Users)`, `Name`, `Address`, `Type (enum: Apartment/House/Condo/Commercial)`, `Description?`, `CreatedAt`, `UpdatedAt` |
| `PropertyInventory` | `Id (Guid)`, `PropertyId (Guid FK)`, `Name`, `Description?`, `Condition (enum: Good/Fair/Poor)`, `CreatedAt` |
| `Lease` | `Id (Guid)`, `PropertyId`, `TenantId (Guid FK→Users)`, `StartDate (DateOnly)`, `EndDate (DateOnly)`, `MonthlyRent (decimal)`, `DepositAmount (decimal)`, `AdvanceAmount (decimal)`, `Status (enum: Active/Terminated/Expired)`, `TenantIdFileUrl?`, `CreatedAt`, `UpdatedAt` |
| `Payment` | `Id (Guid)`, `LeaseId`, `DueDate (DateOnly)`, `AmountDue (decimal)`, `AmountPaid (decimal)`, `Status (enum: Unpaid/Partial/Paid)`, `PaidAt?`, `ProofFileUrl?`, `StripeSessionId?`, `Notes?`, `CreatedAt`, `UpdatedAt` |

On **lease creation**, auto-generate one `Payment` record per month from `StartDate` to `EndDate`.

### Backend Endpoints to Create

```
GET    /api/properties
POST   /api/properties
GET    /api/properties/{id}
PUT    /api/properties/{id}
DELETE /api/properties/{id}

GET    /api/properties/{id}/inventory
POST   /api/properties/{id}/inventory
PUT    /api/properties/{id}/inventory/{itemId}
DELETE /api/properties/{id}/inventory/{itemId}

GET    /api/leases
POST   /api/leases
GET    /api/leases/{id}
PATCH  /api/leases/{id}/terminate

GET    /api/payments                                    (landlord: all theirs; tenant: own lease payments)
GET    /api/payments/{id}
POST   /api/leases/{id}/payments/stripe-checkout        → returns { checkoutUrl }
POST   /api/payments/stripe-webhook                     (AllowAnonymous — validates Stripe-Signature)
POST   /api/payments/{id}/manual-pay                    (proof file URL + notes)

GET    /api/storage/upload-url?bucket=&path=            (returns Supabase signed upload URL)
```

### Authorization Rules

- Landlords: access only their own properties/leases/payments (filter by `LandlordId == currentUserId` in service layer)
- Tenants: only their own lease and payments
- Stripe webhook: `[AllowAnonymous]`, validated by `Stripe-Signature` header against `STRIPE_WEBHOOK_SECRET`

### Frontend Features to Build

1. **Properties** — list table (name, address, type, occupancy status), create/edit form, inventory sub-section on detail page
2. **Leases** — list page, create form (select property, tenant email lookup, date range, amounts, deposit, advance), lease detail page
3. **Payments** — landlord list (filterable by status/property), tenant list, Pay Now → Stripe redirect, manual payment modal (proof URL + notes), status badges (Unpaid=red, Partial=yellow, Paid=green)
4. Update sidebar nav to link Properties, Leases, Payments
5. Add all new endpoints to `ApiService`

---

## Recommended Implementation Order

1. Create EF Core entities in `Entities/`
2. Add `DbSet<>` entries to `AppDbContext`
3. Run `dotnet ef migrations add Wave2Entities` and `dotnet ef database update`
4. Create service layer: `PropertyService`, `LeaseService` (includes payment generation), `PaymentService`
5. Create repositories if needed, or use EF Core directly in services
6. Create controllers: `PropertiesController`, `LeasesController`, `PaymentsController`
7. Add Stripe integration: checkout session + webhook handler
8. Add `StorageController` for signed upload URLs
9. Update `ApiService` in Angular with all new endpoints
10. Build Angular Properties feature (`features/properties/`)
11. Build Angular Leases feature (`features/leases/`)
12. Build Angular Payments feature (`features/payments/`)
13. Update sidebar nav and routing

---

## Before Starting Wave 2

1. Merge or branch off `feature/wave-1-foundation` — do not develop Wave 2 on the same branch
2. Create new branch: `feature/wave-2-properties-leases-payments`
3. Read the existing `AppDbContext`, `AuthService`, and `ApiService` to understand current patterns before adding new code

---

## File Map — Existing Files (Do Not Break)

| File | Purpose |
|---|---|
| `backend/RentalManagementApi/Program.cs` | DI, OIDC auth, CORS, middleware pipeline |
| `backend/RentalManagementApi/Data/AppDbContext.cs` | EF Core context — add new DbSets here |
| `backend/RentalManagementApi/Controllers/AuthController.cs` | `/api/auth` — register, invite, accept-invite |
| `backend/RentalManagementApi/Application/Services/AuthService.cs` | Registration, invite token logic |
| `backend/RentalManagementApi/Application/Services/UserService.cs` | Profile get/update |
| `frontend/.../core/services/auth.service.ts` | Supabase auth, session signals |
| `frontend/.../core/services/api.service.ts` | All HTTP calls — extend for Wave 2 |
| `frontend/.../core/interceptors/auth.interceptor.ts` | Attaches Bearer token to every request |

---

## Environment Variables

Stored in `.env` at the repo root (never committed). All must be present for local dev and configured as Railway/Vercel secrets for deployment.

```
DATABASE_URL           — Session Pooler URI: postgresql://postgres.xxx@aws-1-ap-northeast-1.pooler.supabase.com:5432/postgres
SUPABASE_URL           — https://xxx.supabase.co
SUPABASE_ANON_KEY      — public anon key (safe to use in Angular environment.ts)
JWT_SECRET             — kept for reference, not used for JWT validation (OIDC JWKS handles this)
STRIPE_SECRET_KEY      — sk_test_... (backend only)
STRIPE_WEBHOOK_SECRET  — whsec_... (backend only, for webhook signature validation)
SENDGRID_API_KEY       — SG.... (backend only)
FRONTEND_URL           — http://localhost:4200 local / Vercel URL in prod (used for CORS)
PORT                   — 8080 (Railway injects this automatically)
```

---

## Next Wave After Wave 2

**Wave 3 — Maintenance, Dashboard, Reminders:**
- `MaintenanceRequest` entity + tenant submission + landlord resolution
- Dashboard endpoint with aggregated metrics (occupancy, unpaid rent, open maintenance)
- Dashboard charts (Chart.js or ng2-charts)
- `ReminderSetting` entity + per-lease reminder config
- Reminder job triggered by GitHub Actions daily cron
- SendGrid email templates (rent-due, overdue, welcome, invite)
