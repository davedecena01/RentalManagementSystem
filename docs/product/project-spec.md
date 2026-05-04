# project-spec.md — Rental Property Manager

## README for Claude

**Instructions:** This spec is the single source of truth for the Rental Property Manager app. The frontend is **Angular** (not Next.js or React). The backend is **.NET 8 Web API** (C#). Currency is **PHP (₱)** — hard-coded, no per-property currency field. Tenants are **landlord-invite only** — no tenant self-registration path exists.

When generating code: read the Database Schema section first, then API Contracts, then UI pages. Follow the architecture rules in `docs/technical/system-architecture.md`. All secrets come from environment variables. Never hardcode credentials.

---

## Executive Summary

**Rental Property Manager** is a portfolio-quality, demo-ready web application for Philippine landlords and property managers. It enables landlords to manage properties, tenants, leases, payments, and maintenance requests from a single dashboard.

The app is built to impress freelance/agency clients: polished UX, live deployment with realistic seed data, and three portfolio-elevating differentiators (PDF document generation, audit log/activity feed, landlord onboarding wizard).

**Stack:** Angular + .NET 8 Web API + Supabase (PostgreSQL, Auth, Storage) + Stripe + SendGrid

**Timeline:** 6–8 weeks across 4 waves

**Demo acceptance checklist:**
- Landlord signs up → completes onboarding wizard → portfolio dashboard shows metrics
- Landlord creates properties with inventory, assigns tenants via email invite
- Tenant accepts invite → logs in → sees lease → pays rent via Stripe or uploads proof
- Lease agreement PDF and rent receipts are auto-generated and downloadable
- Maintenance requests submitted by tenant → resolved by landlord
- Audit/activity log shows all key actions in chronological order
- Dashboard charts show monthly income and payment status breakdown
- Rent reminder emails arrive before/after due date per landlord settings

---

## Target Users

| Role | Description |
|------|-------------|
| **Landlord / Property Manager** | Primary user. Signs up to manage properties, tenants, leases, and finances. Invited to use demo via landing page. |
| **Tenant** | Renter. Receives email invite from landlord. Views lease, pays rent, uploads proof, submits maintenance. |

**Out of scope (v2):** Staff/Accountant role — deferred to backlog.

---

## Goals and Success Criteria

- Landlords can manage their full rental workflow from one tool
- Tenants have a clean, transparent view of their obligations
- App is live, publicly accessible, and demoable with realistic seed data
- Code quality demonstrates clean architecture, tested services, and CI/CD
- Three differentiator features visibly elevate it above CRUD-app competitors

---

## MVP Scope — Wave Structure

Features are divided into 4 shippable waves. Each wave ends with a working demo milestone.

### Wave 1 — Foundation & Auth (Weeks 1–2)
- User registration (landlord) via Supabase Auth
- Landlord invite-only tenant onboarding (email invite → token link → tenant sets password)
- Role-based access (Landlord vs. Tenant)
- User profile view and edit
- Angular skeleton + layout shell (sidebar nav, top bar)
- .NET 8 API skeleton with JWT auth, global error handler, `/healthz`
- GitHub Actions CI (build + test on push/PR)
- Backend deployed to Railway; frontend to Vercel

### Wave 2 — Properties, Leases, Payments (Weeks 3–4)
- Property CRUD (address, furnishing type, deposit/advance fields)
- Property inventory items (sub-list on property)
- Tenant ID upload (Supabase Storage, signed URLs)
- Lease assignment (tenant linked to property, monthly rent, start/end dates, deposit/advance)
- Auto-generation of monthly Payment records on lease creation
- Stripe Checkout payment flow (tenant pays online; webhook confirms)
- Manual payment proof upload (tenant uploads screenshot/bank receipt)
- Payment status: Unpaid / Partial / Paid
- Landlord and tenant payment list views

### Wave 3 — Maintenance, Dashboard, Reminders (Weeks 5–6)
- Maintenance request submission by tenant (title, description, photo)
- Landlord maintenance resolution (notes, status update)
- Dashboard with metric cards (properties, occupancy, monthly rent, unpaid rent, open maintenance)
- Dashboard charts: monthly income (last 12 months), paid vs. unpaid breakdown
- Configurable rent reminders per lease (days before / days after due date)
- Email delivery via SendGrid (rent reminder, overdue notice, welcome, invite)
- Scheduled reminder trigger (GitHub Actions daily cron → secured API endpoint)

### Wave 4 — Differentiator Features: PDF + Audit Log + Onboarding (Weeks 7–8)
- **PDF Lease Agreement** — landlord generates signed lease PDF; stored in Supabase Storage
- **PDF Rent Receipt** — auto-generated on payment confirmation; downloadable by landlord and tenant
- **Audit Log / Activity Feed** — every significant action logged; visible on landlord dashboard
- **Landlord Onboarding Wizard** — step-by-step first-run flow for new landlords
- **Demo Seed Data** — realistic seed: 3 properties, 2 tenants, 12 months payments, maintenance, audit entries
- **Polish pass** — loading skeletons, empty states, toast notifications, confirmation dialogs

---

## Differentiator Features (Portfolio Elevators)

These features are explicitly called out because they are what separates this project from a generic CRUD app. Each maps to a real gap vs. free-tier competitors like Innago and TenantCloud.

### 1. PDF Document Generation

**Why it matters:** Buildium and AppFolio generate professional PDFs automatically. Free-tier tools force landlords to manage documents separately.

**Lease Agreement PDF**
- Trigger: landlord clicks "Generate Lease Agreement" on a lease
- Backend: `POST /api/leases/{id}/generate-agreement` using **QuestPDF** (.NET MIT-licensed library)
- Content: parties (landlord and tenant name/email), property address, lease dates, monthly rent (₱), deposit and advance amounts, special terms textarea, signature block placeholder
- Storage: saved to Supabase Storage bucket `lease-documents`; URL saved to `LeaseDocument` record
- UI: "Download Lease PDF" button on lease detail page

**Rent Receipt PDF**
- Trigger: automatic on payment status → Paid (Stripe webhook or manual confirmation)
- Backend: receipt PDF generated by `PaymentService` after marking payment Paid
- Content: receipt number, landlord and tenant names, property address, period covered, amount paid (₱), payment date, payment method (Stripe / Manual)
- Storage: Supabase Storage bucket `lease-documents`; URL saved to `LeaseDocument` with type `Receipt`
- UI: "Download Receipt" button on each Paid payment row

**AC:**
- PDF is readable (not corrupt) and contains accurate data
- Download link visible to both landlord and tenant
- File is stored in Supabase Storage (not served from API memory)

### 2. Audit Log / Activity Feed

**Why it matters:** Enterprise tools (Yardi, Buildium) keep audit trails for compliance. No free-tier tool does this.

**Events logged:**
- Tenant invited
- Tenant accepted invite
- Property created / updated / deleted
- Lease created / updated / terminated
- Payment recorded (manual) / confirmed (Stripe webhook)
- Payment receipt generated
- Lease agreement generated
- Maintenance request submitted / resolved
- Reminder sent

**Data:** `AuditLog` entity with `actor_user_id`, `entity_type`, `entity_id`, `action`, `payload_json` (optional diff), `created_at`

**API:** `GET /api/audit-log?page=1&pageSize=20&entityType=&dateFrom=&dateTo=` — scoped to landlord's entities only

**UI:** "Activity" tab on landlord dashboard — paginated chronological list with entity type icon, description ("Maria Cruz accepted tenant invite"), relative timestamp ("2 hours ago"), and a link to the related entity

**AC:**
- Every listed event type creates an audit entry
- Audit log is scoped: landlord cannot see another landlord's entries
- Entries persist even if the related entity is later deleted (snapshot the relevant display fields in `payload_json`)

### 3. Landlord Onboarding Wizard

**Why it matters:** Premium SaaS like DoorLoop and Buildium guide new users through setup. A blank dashboard is a bad first impression for a demo.

**Trigger:** Landlord logs in for the first time with zero properties → automatically redirected to wizard. "Skip" is always available.

**4-step flow:**
1. **Welcome** — explains the app briefly; shows demo landlord option
2. **Add your first property** — inline property form (address, rent, furnishing type)
3. **Invite your first tenant** — inline email invite form (skippable)
4. **Done** — summary of what was created; CTA to view dashboard

**State:** tracked via `onboarding_completed` boolean on `User` profile

**UI:** full-page step wizard with progress bar, step indicators, back/next navigation, skip option on each step

**AC:**
- First-time landlord sees wizard on login
- Landlord can skip any step and proceed
- Completing or skipping the wizard sets `onboarding_completed = true` → never shown again
- Properties/invites created during wizard persist normally

---

## Backlog (v2 / Post-MVP)

The following features are deliberately deferred. They are documented here so they can be picked up after the 6–8-week build.

| Feature | Why Deferred |
|---------|-------------|
| Staff / Accountant role | Adds auth complexity with low MVP ROI |
| Real-time in-app notifications (Supabase Realtime / SignalR) | Nice-to-have; polling is sufficient for demo |
| Vendor / contractor management | Valuable but adds 1–1.5 weeks of scope |
| Financial reports (P&L, expense tracking, Schedule E CSV export) | Requires expense entity not in MVP |
| Two-factor authentication (TOTP) | Supabase has it; integrate post-launch |
| Dark mode | Low effort but distracts from Wave 4 polish |
| Late fee calculation and automation | Business rule complexity; defer to v2 |
| Multi-tenant lease (joint tenants) | Schema change needed |
| In-app messaging between landlord and tenant | Complexity outweighs MVP value |
| E-signature integration (DocuSign / HelloSign) | Paid service; PDF stub is sufficient |
| SMS notifications | Twilio cost; email is enough for MVP |
| Multi-currency support | Hard-coded PHP is sufficient for demo |
| Bulk import / export | Not needed for demo |
| Google OAuth SSO | Supabase supports it; add post-Wave 1 if desired |

---

## Data Model

Primary keys are `Guid` across all entities. Timestamps are UTC. Money fields are `decimal(12,2)`. Currency is PHP — no currency column needed.

### ERD (Mermaid)

```mermaid
erDiagram
    USERS ||--o{ PROPERTIES : owns
    USERS ||--o{ LEASES : tenant_on
    USERS ||--o{ MAINTENANCEREQUESTS : submits
    USERS ||--o{ AUDITLOGS : actor
    PROPERTIES ||--o{ INVENTORYITEMS : has
    PROPERTIES ||--o{ LEASES : has
    PROPERTIES ||--o{ MAINTENANCEREQUESTS : on
    LEASES ||--o{ PAYMENTS : generates
    LEASES ||--o{ LEASEDOCUMENTS : produces
    PAYMENTS ||--o{ LEASEDOCUMENTS : receipt_for

    USERS {
        Guid id PK
        string email UNIQUE
        string fullName
        string phone
        enum role "Landlord|Tenant"
        bool onboardingCompleted
        string tenantIdUrl
        datetime createdAt
        datetime updatedAt
    }
    PROPERTIES {
        Guid id PK
        Guid landlordId FK
        string name
        string address
        string city
        string province
        string zipCode
        string description
        decimal monthlyRent
        enum furnishingType "Bare|SemiFurnished|FullyFurnished"
        bool requiresDeposit
        decimal depositAmount
        bool requiresAdvance
        int advanceMonths
        datetime createdAt
        datetime updatedAt
    }
    INVENTORYITEMS {
        Guid id PK
        Guid propertyId FK
        string itemName
        int quantity
        string condition
        string notes
        datetime createdAt
    }
    LEASES {
        Guid id PK
        Guid propertyId FK
        Guid tenantId FK
        date startDate
        date endDate
        decimal monthlyRent
        bool depositTaken
        decimal depositAmount
        bool advanceTaken
        int advanceMonths
        enum status "Active|Expired|Terminated"
        int reminderBeforeDays
        int reminderAfterDays
        bool remindersEnabled
        datetime createdAt
        datetime updatedAt
    }
    PAYMENTS {
        Guid id PK
        Guid leaseId FK
        date dueDate
        decimal amountDue
        decimal amountPaid
        enum status "Unpaid|Partial|Paid"
        string stripeSessionId
        string proofFileUrl
        string notes
        datetime paidAt
        datetime createdAt
        datetime updatedAt
    }
    MAINTENANCEREQUESTS {
        Guid id PK
        Guid propertyId FK
        Guid tenantId FK
        string title
        string description
        string imageUrl
        enum status "Open|InProgress|Resolved|Closed"
        enum priority "Low|Medium|High|Urgent"
        string resolutionNotes
        datetime resolvedAt
        datetime createdAt
        datetime updatedAt
    }
    LEASEDOCUMENTS {
        Guid id PK
        Guid leaseId FK
        Guid paymentId FK "null for lease agreements"
        enum type "LeaseAgreement|Receipt"
        string fileUrl
        datetime generatedAt
    }
    AUDITLOGS {
        Guid id PK
        Guid actorUserId FK
        string entityType
        string entityId
        string action
        string payloadJson
        datetime createdAt
    }
    PAYMENTWEBHOOKEVENTS {
        Guid id PK
        string stripeEventId UNIQUE
        string eventType
        string rawPayload
        datetime processedAt
    }
    REMINDERSETTINGS {
        Guid id PK
        Guid leaseId FK
        int daysBefore
        int daysAfter
        bool isEnabled
    }
```

### EF Core Migration Notes

- Use `Guid` for all PKs; configure `HasDefaultValueSql("gen_random_uuid()")`
- Add indexes: `LandlordId` on Properties; `PropertyId`, `TenantId` on Leases; `LeaseId`, `DueDate`, `Status` on Payments; `PropertyId`, `Status` on MaintenanceRequests; `CreatedAt` on AuditLogs; `StripeEventId` UNIQUE on PaymentWebhookEvents
- Use `HasConversion<string>()` for enums or store as `nvarchar` with explicit values
- Use `decimal(12,2)` for all money fields
- Avoid cascade deletes on Payments and AuditLogs — prefer service-level cleanup

---

## API Contract Summary

Full contracts in `docs/technical/api-contracts.md`. Endpoints grouped by resource:

| Group | Endpoints |
|-------|-----------|
| Auth | `POST /api/auth/register`, `POST /api/auth/invite-tenant`, `POST /api/auth/accept-invite` |
| Users | `GET /api/users/me`, `PATCH /api/users/me` |
| Properties | `GET`, `POST /api/properties`; `GET`, `PUT`, `DELETE /api/properties/{id}` |
| Inventory | `GET`, `POST /api/properties/{id}/inventory`; `PUT`, `DELETE /api/properties/{id}/inventory/{itemId}` |
| Leases | `GET`, `POST /api/leases`; `GET`, `PUT`, `PATCH status /api/leases/{id}`; `GET /api/leases/my` |
| Payments | `GET`, `GET /{id}`, `PATCH /{id}/manual /api/payments`; `GET /api/payments/my` |
| Stripe | `POST /api/payments/stripe/checkout-session`; `POST /api/payments/stripe/webhook` |
| Documents | `POST /api/leases/{id}/generate-agreement`; `POST /api/payments/{id}/generate-receipt`; `GET /api/leases/{id}/documents` |
| Maintenance | `GET`, `POST /api/maintenance`; `GET`, `PATCH status /api/maintenance/{id}` |
| Dashboard | `GET /api/dashboard` |
| Audit Log | `GET /api/audit-log` |
| Reminders | `GET`, `PUT /api/reminders/{leaseId}`; `POST /api/reminders/trigger` |
| Files | `POST /api/files/upload` |
| Health | `GET /healthz` |

**Auth rules:** All endpoints require `Authorization: Bearer <supabase-jwt>` except `/api/auth/*`, `/api/payments/stripe/webhook`, and `/healthz`. Stripe webhook validates `Stripe-Signature` header — no JWT.

---

## User Stories and Acceptance Criteria

### 1. User Authentication & Roles

**Landlord registration:**
- AC: landlord registers with name, email, password → Supabase Auth creates user → backend creates profile → landlord redirected to onboarding wizard

**Tenant invite flow (landlord-invite only — no self-register):**
- AC: landlord submits tenant email → backend sends invite email with secure token link → tenant clicks link → lands on "Set your password" page → after setting password, lands on tenant dashboard showing their lease

**Role-based access:**
- AC: landlord routes are blocked from tenants; tenant routes are blocked from landlords; redirect to dashboard on unauthorized route access

**Demo:** Two public demo logins (landlord@demo.com / tenant@demo.com) documented on `/demo` page.

### 2. Property Management

**AC:**
- Landlord creates property: name, address (city, province, zip), monthly rent (₱), furnishing type, deposit settings, advance settings
- Inventory items added inline (item name, quantity, condition, notes)
- Edit and delete work; delete blocked if property has an active lease
- Tenant sees their assigned property details (read-only)

**Demo:** Create "Sunset Studio 1A" in Cebu City, ₱8,000/month, Semi-Furnished, add TV + Air Conditioner to inventory.

### 3. Tenant Invite & Lease Assignment

**AC:**
- Landlord selects property → "Assign Tenant" → enters tenant email → system sends invite
- On invite acceptance, landlord creates lease: start date, end date, monthly rent, deposit/advance
- Tenant uploads ID (image or PDF); stored as signed URL in Supabase Storage
- System auto-generates monthly Payment records from start to end date on lease creation

**Demo:** Invite Maria Cruz (maria@demo.com) to Sunset Studio → she accepts → landlord creates 12-month lease at ₱8,000/month → 12 payment records appear.

### 4. Online & Manual Rent Payments

**AC:**
- Tenant sees payment list with month, amount due (₱), amount paid, status badge
- "Pay Now" → Stripe Checkout → on success, Stripe webhook marks payment Paid, auto-generates receipt PDF
- "Upload Proof" → tenant attaches image; landlord reviews and manually marks Paid/Partial
- Partial payment: landlord records amountPaid < amountDue → status = Partial
- Color codes: green = Paid, orange = Partial, red = Unpaid

**Demo:** Test Stripe payment (card 4242 4242 4242 4242) → payment turns green → receipt PDF available for download.

### 5. Maintenance Requests

**AC:**
- Tenant submits request: title, description, priority (Low/Medium/High/Urgent), optional photo
- Landlord sees list of all requests across their properties, filterable by status and property
- Landlord updates status (Open → InProgress → Resolved) with optional resolution notes
- Audit log records submission and resolution events

**Demo:** Tenant submits "Leaky faucet in kitchen" → landlord marks InProgress → then Resolved with note "Replaced washer."

### 6. Dashboard & Analytics

**AC:**
- Cards: Total Properties, Occupied, Vacant, Total Monthly Rent (₱), Total Unpaid Rent (₱), Open Maintenance Requests
- Monthly Income chart: bar chart of AmountPaid per month over last 12 months
- Payment Status chart: doughnut — count of Paid / Partial / Unpaid for current period
- All data computed server-side, not guessed on frontend

**Demo:** With seed data loaded, dashboard shows meaningful numbers within 2 seconds of login.

### 7. Rent Reminders

**AC:**
- Landlord configures per-lease: days before due date to send reminder, days after due to send overdue notice, toggle enabled/disabled
- Daily cron job triggers `POST /api/reminders/trigger` with `X-Internal-Key` header
- System sends email via SendGrid to tenants with unpaid rent meeting the criteria
- Reminder attempts are logged in AuditLog
- No duplicate sends (idempotency by lease+payment+reminder_type per day)

**Demo:** Show SendGrid activity log with a sent reminder email.

### 8. PDF Document Generation

**AC:** (see Differentiator Features section above)

### 9. Audit Log

**AC:** (see Differentiator Features section above)

### 10. Onboarding Wizard

**AC:** (see Differentiator Features section above)

### 11. User Profile & Settings

**AC:**
- Landlord/tenant can view and update name and phone number
- Tenant can re-upload their ID document
- Password change handled via Supabase Auth "forgot password" flow

---

## Non-Functional Requirements

| Concern | Requirement |
|---------|-------------|
| **Security** | HTTPS everywhere; JWT auth on all protected endpoints; file type/size validation; no raw card data (Stripe Checkout); signed URLs for sensitive files |
| **Privacy** | Tenant ID files accessible only via signed URLs to authorized users (landlord + that tenant); no PII in logs |
| **Performance** | DB indexes on all common filters; paginated list results; dashboard queries optimized (single aggregation query) |
| **Reliability** | Stripe webhook idempotency; reminder deduplication; supabase-free-tier keep-alive ping via cron |
| **Logging** | Serilog (console output captured by Railway); request/response logging middleware; AppLog entity for application events |
| **Error responses** | All errors: `{ "error": "human message", "code": "ERROR_CODE" }`; no stack traces exposed |
| **Test coverage** | >70% coverage on backend services and controllers; all tests must pass in CI |
| **Accessibility** | WCAG 2.1 AA contrast ratios; keyboard-navigable forms; meaningful alt text |

---

## Tech Stack

| Layer | Technology | Notes |
|-------|-----------|-------|
| Frontend | Angular (latest stable) + TypeScript + SCSS | **Not** Next.js or React |
| Backend | .NET 8 Web API, C# | EF Core + Npgsql |
| Database | Supabase PostgreSQL | Free tier |
| Auth | Supabase Auth (email/password) | JWT RS256 validated in .NET |
| Storage | Supabase Storage | tenant-ids, payment-proofs, maintenance-images, lease-documents buckets |
| PDF generation | **QuestPDF** (.NET, MIT license) | Generates lease agreements and receipts |
| Payments | Stripe Checkout | No raw card data |
| Email | SendGrid | Free tier: 100 emails/day |
| Frontend hosting | Vercel | Auto-deploy from main |
| Backend hosting | Railway (or Render / Fly.io) | Docker or Nixpacks |
| CI | GitHub Actions | Build + test on push/PR; daily cron for reminders |
| Currency | **PHP (₱)** hard-coded | No multi-currency in MVP |

---

## Project Structure

```
RentalManagementSystem/
├── backend/
│   └── RentalManagementApi/
│       ├── Controllers/
│       ├── Application/Services/
│       ├── Data/
│       │   ├── AppDbContext.cs
│       │   ├── Migrations/
│       │   ├── Repositories/
│       │   └── Seeders/
│       ├── Entities/
│       ├── DTOs/
│       │   ├── Requests/
│       │   └── Responses/
│       ├── Integrations/
│       │   ├── Stripe/
│       │   ├── SendGrid/
│       │   ├── Supabase/
│       │   └── Pdf/
│       ├── Options/
│       ├── Validation/
│       ├── Middleware/
│       ├── Jobs/
│       └── Common/
├── frontend/
│   └── rental-management-ui/
│       └── src/app/
│           ├── core/
│           ├── shared/
│           └── features/
│               ├── auth/
│               ├── dashboard/
│               ├── properties/
│               ├── leases/
│               ├── payments/
│               ├── maintenance/
│               ├── audit-log/
│               └── account/
├── docs/
│   ├── product/project-spec.md  ← this file
│   ├── technical/
│   │   ├── system-architecture.md
│   │   ├── api-contracts.md
│   │   └── deployment.md
│   └── planning/
│       └── execution-plan.md
├── .github/workflows/
├── .env.example
└── README.md
```

---

## Sprint Plan — 4 Waves (6–8 Weeks)

| Wave | Focus | Weeks | End-State Demo |
|------|-------|-------|----------------|
| 1 | Foundation & Auth | 1–2 | Landlord signs up, invites tenant, both log in; live on Railway + Vercel |
| 2 | Properties, Leases, Payments | 3–4 | Full payment workflow: Stripe + manual; lease assigned with auto-generated payments |
| 3 | Maintenance, Dashboard, Reminders | 5–6 | Dashboard tells the portfolio story; reminders send; maintenance resolved |
| 4 | PDF + Audit Log + Onboarding + Polish | 7–8 | Lease PDF downloadable; audit feed shows history; wizard guides new landlord; seed data live |

---

## Open Questions (Unresolved)

| # | Question | Impact if Changed |
|---|----------|------------------|
| 1 | Use Hangfire or Railway Cron for reminder job? | Affects deployment and reliability of reminder scheduling |
| 2 | Is a `TenantProfile` extension table needed, or store tenant ID URL on `User`? | Minor schema decision |
| 3 | Should deleted properties/leases be soft-deleted (IsDeleted flag) or hard-deleted? | Affects audit log accuracy and cascade rules |

---

## Resolved Decisions

| Decision | Choice | Date |
|----------|--------|------|
| Frontend framework | Angular | 2026-05-05 |
| Currency | PHP (₱), hard-coded | 2026-05-05 |
| Tenant onboarding | Landlord invite only (email link) | 2026-05-05 |
| Portfolio audience | Freelance/agency clients | 2026-05-05 |
| Scope | MVP + 3 standout features (6–8 weeks) | 2026-05-05 |
| Standout feature group | Polish & Trust: PDF, Audit Log, Onboarding Wizard | 2026-05-05 |
| Demo strategy | Live deployment with seed data and public demo logins | 2026-05-05 |
| Primary key type | Guid | 2026-05-05 |
| PDF library | QuestPDF (MIT, .NET 8 compatible) | 2026-05-05 |
