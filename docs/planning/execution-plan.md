# Execution Plan — Rental Property Manager

## Structure

4 shippable waves (6–8 weeks). Each wave ends with a working demo milestone. Backlog items are listed after Wave 4.

---

## Wave 1 — Foundation & Auth (Weeks 1–2)

**Goal:** A landlord can sign up, log in, and reach an empty dashboard. A tenant can accept an invite and land on their dashboard. Both apps are live on Vercel + Railway.

### Backend
- [ ] Create .NET 8 Web API project (`RentalManagementApi`)
- [ ] Add NuGet packages: `EF Core`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Stripe.net`, `SendGrid`, `Serilog.AspNetCore`, `QuestPDF`, `Swashbuckle.AspNetCore`
- [ ] Configure folder structure (Controllers, Application/Services, Data, Entities, DTOs, Integrations, Middleware, Options, Jobs, Common)
- [ ] Set up `AppDbContext` with Supabase PostgreSQL connection
- [ ] Configure Supabase JWT validation (RS256, validate against Supabase JWKS)
- [ ] Add `ErrorHandlingMiddleware` (catch unhandled exceptions, return standard error JSON)
- [ ] Add `RequestLoggingMiddleware` (log method, path, status, duration)
- [ ] Add `/healthz` endpoint
- [ ] Configure `IOptions<T>` strongly-typed options for Supabase, Stripe, SendGrid
- [ ] Add `User` entity and EF Core migration
- [ ] `POST /api/auth/register` — create backend user profile after Supabase signup
- [ ] `POST /api/auth/invite-tenant` — generate invite token, send SendGrid invite email
- [ ] `POST /api/auth/accept-invite` — validate token, create tenant profile
- [ ] `GET /api/users/me` — return authenticated user profile
- [ ] `PATCH /api/users/me` — update profile (name, phone, onboardingCompleted)
- [ ] Role-based authorization policies (`LandlordPolicy`, `TenantPolicy`)
- [ ] Unit tests: register validation, invite token generation, JWT auth returns 401

### Frontend
- [ ] Create Angular project (`rental-management-ui`)
- [ ] Configure `environments/environment.ts` and `environment.prod.ts`
- [ ] Set up folder structure (core, shared, features/*)
- [ ] Add JWT interceptor (`core/interceptors/jwt.interceptor.ts`)
- [ ] Add auth guard (`core/auth/auth.guard.ts`) for role-based route protection
- [ ] Set up `AuthService` wrapping `@supabase/supabase-js`
- [ ] Login page (email + password, validation)
- [ ] Register page (name, email, password, confirm password)
- [ ] Forgot password page (calls Supabase reset)
- [ ] Accept invite page (`/accept-invite?token=...`)
- [ ] Layout shell: left sidebar nav + top bar with user menu
- [ ] Empty dashboard placeholder (cards showing 0 / dashes)
- [ ] Empty tenant dashboard placeholder
- [ ] Toast notification service
- [ ] Angular Router with lazy-loaded feature modules
- [ ] Tests: login form validation, register form validation, auth guard redirects

### Infrastructure
- [ ] Create Supabase project; note URL, anon key, service role key, JWT secret
- [ ] Create 4 Storage buckets (tenant-ids, payment-proofs, maintenance-images, lease-documents) — all private
- [ ] Configure Supabase Auth: enable email provider, set site URL
- [ ] Create Stripe account; note test keys
- [ ] Create SendGrid account; verify sender email; note API key
- [ ] Push project to GitHub
- [ ] Set up GitHub Actions CI workflow (build + test on push/PR)
- [ ] Deploy backend to Railway (set all env vars)
- [ ] Deploy frontend to Vercel (set env vars, add vercel.json)

**Wave 1 Demo:** Landlord registers at live URL → lands on empty dashboard → invites `maria@example.com` → Maria clicks email link → sets password → lands on tenant dashboard showing her name.

---

## Wave 2 — Properties, Leases, Payments (Weeks 3–4)

**Goal:** Full core landlord workflow. Create a property, assign a tenant via lease, generate payment schedule, accept payment via Stripe and manual upload.

### Backend
- [ ] Add `Property`, `PropertyInventory` entities and migration
- [ ] `GET /api/properties` (paginated, landlord-scoped)
- [ ] `POST /api/properties`
- [ ] `GET /api/properties/{id}`
- [ ] `PUT /api/properties/{id}`
- [ ] `DELETE /api/properties/{id}` (blocked if active lease exists → 409)
- [ ] `GET|POST /api/properties/{id}/inventory`
- [ ] `PUT|DELETE /api/properties/{propertyId}/inventory/{itemId}`
- [ ] Add `Lease`, `ReminderSetting` entities and migration
- [ ] `GET /api/leases` (landlord-scoped)
- [ ] `POST /api/leases` (validates dates, auto-generates monthly Payment records)
- [ ] `GET /api/leases/{id}`
- [ ] `PUT /api/leases/{id}`
- [ ] `PATCH /api/leases/{id}/status`
- [ ] `GET /api/leases/my` (tenant-only)
- [ ] Lease date validation: start < end, no overlapping leases for same property
- [ ] Add `Payment`, `PaymentWebhookEvent` entities and migration
- [ ] `GET /api/payments` (landlord-scoped, filtered by leaseId/status)
- [ ] `GET /api/payments/{id}`
- [ ] `PATCH /api/payments/{id}/manual` (landlord marks Partial/Paid with optional proof URL)
- [ ] `GET /api/payments/my` (tenant-only)
- [ ] `POST /api/payments/stripe/checkout-session` (creates Stripe Checkout Session)
- [ ] `POST /api/payments/stripe/webhook` (validates signature, handles `checkout.session.completed`, idempotency check)
- [ ] `POST /api/files/upload` (validates type/size, returns signed Supabase Storage upload URL)
- [ ] Unit tests: property CRUD authorization, lease date validation, payment status calculation, Stripe webhook idempotency

### Frontend
- [ ] Property list page (table with name, rent, occupancy badge, actions)
- [ ] Property create/edit form (all fields, dynamic inventory list)
- [ ] Property detail page (info, inventory tab, lease tab, payments tab)
- [ ] Lease create form (tenant dropdown, dates, rent, deposit/advance)
- [ ] Lease detail view (tenant info, dates, rent, status)
- [ ] Payment list (table with month, amount due, amount paid, status badge, actions)
- [ ] "Pay Now" button → Stripe Checkout redirect (tenant only)
- [ ] Stripe success/cancel return pages
- [ ] Manual payment form (amount, notes, file upload)
- [ ] File upload component (input + preview + progress)
- [ ] Tenant lease view (`/my-lease`) with payment list
- [ ] Status badges (Unpaid=red, Partial=orange, Paid=green)
- [ ] Loading skeletons on all list/detail pages
- [ ] Empty-state components (e.g. "No properties yet — add your first one")
- [ ] Confirmation dialogs for delete actions
- [ ] Tests: property form validation, lease date validation, payment status display

**Wave 2 Demo:** Landlord creates "Sunset Studio 1A" in Cebu City → adds TV and aircon to inventory → invites Maria → creates 12-month lease at ₱8,000/month → payment schedule generates → Maria logs in → clicks "Pay Now" for January → Stripe test card 4242... → payment turns green.

---

## Wave 3 — Maintenance, Dashboard, Reminders (Weeks 5–6)

**Goal:** The app feels complete. Dashboard tells the portfolio story. Tenants can report issues, landlords resolve them. Reminders send automatically every night.

### Backend
- [ ] Add `MaintenanceRequest` entity and migration
- [ ] `GET /api/maintenance` (landlord-scoped, filterable by property/status)
- [ ] `POST /api/maintenance` (tenant-only; validates tenant is assigned to property)
- [ ] `GET /api/maintenance/{id}`
- [ ] `PATCH /api/maintenance/{id}/status` (landlord-only)
- [ ] `GET /api/dashboard` — aggregate: properties, occupancy, monthly rent, unpaid rent, open maintenance, monthly income chart data, payment status breakdown
- [ ] `GET|PUT /api/reminders/{leaseId}`
- [ ] `POST /api/reminders/trigger` (secured by `X-Internal-Key` header)
- [ ] `ReminderJob`: select unpaid/partial payments matching reminder criteria, send SendGrid emails, log attempts (deduplication: one send per lease+due_date+type per day)
- [ ] Unit tests: dashboard aggregation, reminder candidate selection, deduplication logic

### Frontend
- [ ] Maintenance submit form (tenant view — title, description, priority, photo)
- [ ] Maintenance list page (landlord view — table, filter by status/property)
- [ ] Maintenance detail / resolve form (landlord)
- [ ] Priority and status badges
- [ ] Dashboard metric cards (6 cards)
- [ ] Monthly income bar chart (ng2-charts or Chart.js)
- [ ] Paid vs. unpaid doughnut chart
- [ ] Reminder settings page (per-lease toggles and day inputs)
- [ ] Tests: dashboard card rendering, maintenance form validation, chart rendering with mock data

### Infrastructure
- [ ] GitHub Actions reminder cron workflow (`.github/workflows/reminders.yml`)
- [ ] Add `API_URL` and `INTERNAL_API_KEY` to GitHub secrets

**Wave 3 Demo:** Tenant submits "Leaky faucet" with photo → landlord sees it on maintenance page → marks InProgress → then Resolved with note → dashboard shows 0 open requests. GitHub Actions triggers reminder → SendGrid logs show delivered email.

---

## Wave 4 — PDF Docs + Audit Log + Onboarding + Polish (Weeks 7–8)

**Goal:** Three differentiators that elevate this above CRUD-app competitors. Live demo with seed data and a public demo page.

### Backend
- [ ] Add `LeaseDocument` entity and migration
- [ ] `DocumentService` with QuestPDF:
  - `GenerateLeaseAgreementAsync(leaseId)` — creates lease PDF (parties, property, lease terms, deposit/advance, signature block)
  - `GenerateReceiptAsync(paymentId)` — creates receipt PDF (receipt number, parties, amount paid, period, method, date)
  - Both upload to `lease-documents` Supabase Storage bucket and save `LeaseDocument` record
- [ ] `POST /api/leases/{id}/generate-agreement`
- [ ] `POST /api/payments/{id}/generate-receipt`
- [ ] `GET /api/leases/{id}/documents`
- [ ] Auto-trigger `GenerateReceiptAsync` in `PaymentService.MarkAsPaidAsync` (Stripe webhook and manual)
- [ ] Add `AuditLog` entity and migration
- [ ] `AuditLogService.LogAsync(actorId, entityType, entityId, action, payloadJson)` — called from each service on meaningful actions
- [ ] Audit events: tenant invited, invite accepted, property created/updated/deleted, lease created/updated/terminated, payment recorded/confirmed, receipt generated, agreement generated, maintenance submitted/resolved, reminder sent
- [ ] `GET /api/audit-log` (landlord-scoped, paginated, filterable by entityType and date range)
- [ ] `DatabaseSeeder` — creates full demo dataset (see deployment.md for details)
- [ ] Unit tests: PDF generation produces non-empty bytes, audit log entries created on each event, seeder produces expected counts

### Frontend
- [ ] `POST /api/leases/{id}/generate-agreement` → "Generate Lease PDF" button on lease detail
- [ ] "Download Receipt" button on Paid payment rows (opens signed URL)
- [ ] Documents list section on lease detail page
- [ ] Audit Log page (`/audit-log`) — paginated, filterable list with entity type icons and relative timestamps
- [ ] Activity Feed widget on landlord dashboard (last 5 entries)
- [ ] Onboarding wizard (4 steps: Welcome, Add Property, Invite Tenant, Done)
  - First-time login redirect logic (check `onboardingCompleted === false` and `properties count === 0`)
  - Step progress bar
  - Skip button on each step
  - Back/Next navigation
- [ ] `/demo` public page showing demo credentials ("Try as Landlord" / "Try as Tenant" buttons that auto-fill login form)
- [ ] Polish pass:
  - Loading skeletons on every async-loaded section
  - Custom empty-state components (icon + message + CTA) for all entity lists
  - Toast notifications for every success/error
  - Confirmation dialogs for all destructive actions (delete property, terminate lease)
  - Consistent ₱ formatting via `CurrencyPhpPipe`
- [ ] Tests: wizard step navigation, audit log entry display, PDF download link visibility

### Infrastructure
- [ ] Set `DEMO_MODE=true` + `PDF_STORAGE_BUCKET=lease-documents` in Railway
- [ ] Trigger seed on first deploy (or via `POST /api/seed` gated by `DEMO_MODE`)
- [ ] Verify all 4 storage buckets are private in Supabase
- [ ] Confirm Stripe webhook is registered for production URL
- [ ] Document public demo credentials in README

**Wave 4 Demo:** Visitor opens live URL → `/demo` page shows credentials → clicks "Try as Landlord" → onboarding wizard plays for a new account, OR logs in as `landlord@demo.com` → sees full dashboard with realistic data → downloads lease PDF → checks audit log → tries "Try as Tenant" → sees payments and downloads receipt.

---

## Backlog (Post-MVP / v2 Candidates)

| Feature | Priority | Notes |
|---------|----------|-------|
| Real-time notifications (Supabase Realtime) | High v2 | Supabase has SDK support; add after Wave 4 |
| Dark mode | Medium v2 | CSS custom properties, 1-2 days of work |
| Financial reports (P&L, expense tracking) | Medium v2 | Requires expense entity |
| Two-factor auth (TOTP) | Medium v2 | Supabase supports it natively |
| Vendor / contractor management | Low v2 | Maintenance assignment to vendors |
| Staff / Accountant role | Low v2 | Additional auth policy and scoped views |
| Late fee calculation | Low v2 | Business rule; needs fee entity |
| Google OAuth SSO | Low v2 | Supabase natively supports it |
| E-signature | Low v2 | DocuSign/HelloSign — paid service |
| Multi-currency | Low v2 | Remove hard-coded PHP constant |
| Multi-tenant lease | Low v2 | Schema change required |
| In-app messaging | Low v2 | Scope vs. value for MVP |
| SMS notifications | Backlog | Twilio cost; email sufficient for MVP |
| Bulk import/export | Backlog | Not needed for demo |

---

## Phase Dependencies

```
Wave 1 (Auth + Infra)
  └─► Wave 2 (Properties + Leases + Payments)
        └─► Wave 3 (Maintenance + Dashboard + Reminders)
              └─► Wave 4 (PDF + Audit Log + Onboarding + Polish + Seed)
```

All waves build on the previous. Do not start Wave 2 until Wave 1 is deployed to live.
