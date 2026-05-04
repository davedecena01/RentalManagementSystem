# System Architecture — Rental Property Manager

## High-Level Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     Browser (Angular SPA)                       │
│  features/auth  dashboard  properties  leases  payments         │
│  maintenance  audit-log  account  shared  core                  │
└─────────────────────────┬───────────────────────────────────────┘
                          │ HTTPS / REST + JWT Bearer
┌─────────────────────────▼───────────────────────────────────────┐
│                    .NET 8 Web API (C#)                          │
│  Controllers → Services → Repositories → EF Core               │
│  Middleware: JWT Auth, Global Error Handler, Request Logging     │
└──────┬─────────────┬──────────────┬──────────────┬─────────────┘
       │             │              │              │
  ┌────▼────┐  ┌─────▼─────┐ ┌────▼────┐  ┌─────▼──────┐
  │Supabase │  │  Supabase  │ │ Stripe  │  │ SendGrid   │
  │PostgreSQL│  │  Storage   │ │Checkout │  │   Email    │
  └─────────┘  └───────────┘ │ Webhook │  └────────────┘
                              └─────────┘
```

**Currency:** PHP (₱) — hard-coded constant. No currency column in any table.
**Primary keys:** `Guid` across all entities.
**Timestamps:** UTC (`DateTime` in C#, mapped to `timestamp with time zone` in Postgres).

---

## Frontend Architecture

### Technology

- **Angular** (latest stable) with TypeScript — **not** Next.js or React
- **SCSS** for styling; Angular Material or custom component library
- **Angular Reactive Forms** for all forms
- **Angular HttpClient** with a JWT interceptor
- **Supabase JS Client** (`@supabase/supabase-js`) for auth session management only

### Folder Structure

```
src/
├── app/
│   ├── core/
│   │   ├── auth/
│   │   │   ├── auth.service.ts          # Supabase auth calls, JWT storage
│   │   │   └── auth.guard.ts            # CanActivate guard
│   │   ├── interceptors/
│   │   │   └── jwt.interceptor.ts       # Attach Bearer token to all API calls
│   │   └── services/
│   │       └── toast.service.ts
│   ├── shared/
│   │   ├── components/
│   │   │   ├── status-badge/
│   │   │   ├── confirm-dialog/
│   │   │   ├── loading-skeleton/
│   │   │   └── empty-state/
│   │   └── pipes/
│   │       └── currency-php.pipe.ts     # Formats ₱ values
│   └── features/
│       ├── auth/
│       │   ├── login/
│       │   ├── register/
│       │   ├── forgot-password/
│       │   ├── accept-invite/           # Tenant invite acceptance page
│       │   └── onboarding-wizard/       # First-run landlord wizard (Wave 4)
│       ├── dashboard/                   # Metric cards + charts
│       ├── properties/                  # Property CRUD + inventory
│       ├── leases/                      # Lease assignment and detail
│       ├── payments/                    # Payment list, Stripe, manual proof
│       ├── maintenance/                 # Submit (tenant) and resolve (landlord)
│       ├── audit-log/                   # Activity feed (landlord only, Wave 4)
│       └── account/                     # Profile and settings
├── environments/
│   ├── environment.ts
│   └── environment.prod.ts
└── styles/
    └── global.scss
```

### Key Responsibilities

| Layer | Responsibility | Must NOT |
|-------|---------------|---------|
| Feature components | UI rendering, user interaction, form handling | Contain business logic or call API directly |
| Feature services | HTTP calls to API, map DTOs to view models | Call Supabase directly (except AuthService) |
| Core guards | Route protection by auth state and role | Allow unauthenticated access to protected routes |
| JWT interceptor | Attach `Authorization: Bearer <token>` to every outgoing request | Store the JWT itself (handled by Supabase client) |
| Shared components | Reusable UI: badges, skeletons, dialogs, empty states | Contain feature-specific logic |

### Angular Conventions

- All API calls through feature services (e.g., `PropertyService`, `PaymentService`)
- `AsyncPipe` preferred over manual subscriptions
- `OnPush` change detection for list components
- Loading states managed per-component with `isLoading: boolean` field
- Error states shown inline (not just toasts)
- Confirmation dialogs for all destructive actions
- Custom `CurrencyPhpPipe` for consistent ₱ formatting across the app

---

## Backend Architecture

### Technology

- **.NET 8 Web API** (C#)
- **Entity Framework Core 8** with `Npgsql` provider
- **Supabase JWT** validation using RS256 public keys (fetched from Supabase JWKS endpoint)
- **QuestPDF** for PDF generation (MIT license)
- **Stripe.net** SDK
- **SendGrid** C# SDK
- **Serilog** for structured logging

### Folder Structure

```
RentalManagementApi/
├── Controllers/
│   ├── AuthController.cs
│   ├── UsersController.cs
│   ├── PropertiesController.cs
│   ├── LeasesController.cs
│   ├── PaymentsController.cs
│   ├── MaintenanceController.cs
│   ├── DashboardController.cs
│   ├── AuditLogController.cs
│   ├── RemindersController.cs
│   └── FilesController.cs
├── Application/
│   └── Services/
│       ├── AuthService.cs
│       ├── PropertyService.cs
│       ├── LeaseService.cs
│       ├── PaymentService.cs
│       ├── MaintenanceService.cs
│       ├── DashboardService.cs
│       ├── AuditLogService.cs
│       ├── ReminderService.cs
│       └── DocumentService.cs          # PDF generation + storage
├── Data/
│   ├── AppDbContext.cs
│   ├── Migrations/
│   ├── Repositories/
│   │   ├── PropertyRepository.cs
│   │   ├── LeaseRepository.cs
│   │   ├── PaymentRepository.cs
│   │   ├── MaintenanceRepository.cs
│   │   └── AuditLogRepository.cs
│   └── Seeders/
│       └── DatabaseSeeder.cs           # Wave 4: demo seed data
├── Entities/
│   ├── User.cs
│   ├── Property.cs
│   ├── PropertyInventory.cs
│   ├── Lease.cs
│   ├── Payment.cs
│   ├── MaintenanceRequest.cs
│   ├── LeaseDocument.cs                # Wave 4: PDF records
│   ├── AuditLog.cs                     # Wave 4
│   ├── ReminderSetting.cs
│   └── PaymentWebhookEvent.cs
├── DTOs/
│   ├── Requests/
│   └── Responses/
├── Integrations/
│   ├── Stripe/
│   │   ├── StripeService.cs
│   │   └── StripeWebhookHandler.cs
│   ├── SendGrid/
│   │   └── EmailService.cs
│   ├── Supabase/
│   │   └── SupabaseStorageService.cs
│   └── Pdf/
│       ├── LeaseAgreementDocument.cs   # QuestPDF document definition
│       └── ReceiptDocument.cs          # QuestPDF document definition
├── Options/
│   ├── SupabaseOptions.cs
│   ├── StripeOptions.cs
│   └── SendGridOptions.cs
├── Middleware/
│   ├── ErrorHandlingMiddleware.cs
│   └── RequestLoggingMiddleware.cs
├── Jobs/
│   └── ReminderJob.cs                  # Called by reminder trigger endpoint
├── Common/
│   ├── Constants.cs
│   ├── Enums.cs
│   └── Extensions/
└── Program.cs
```

### Layering Rules

| Layer | Does | Must NOT |
|-------|------|---------|
| Controller | Parse HTTP request, call one service method, return HTTP result | Contain business logic, call repositories, use EF Core directly |
| Service | Orchestrate business rules, call repositories, call integrations, write audit logs | Depend on `HttpContext` |
| Repository | EF Core queries and writes, no business logic | Call other services or integrations |
| Integration | Stripe, SendGrid, Supabase calls | Be called from repositories |
| DTO | Shape API request/response contracts | Reference EF Core entities |

---

## Database Schema (Full)

Currency is PHP — no currency column anywhere. All money: `decimal(12,2)`.

### Users
```sql
Users (
  Id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  Email         varchar UNIQUE NOT NULL,
  FullName      varchar NOT NULL,
  Phone         varchar,
  Role          varchar NOT NULL,          -- 'Landlord' | 'Tenant'
  OnboardingCompleted boolean DEFAULT false,
  TenantIdUrl   varchar,                   -- signed Supabase Storage URL
  CreatedAt     timestamptz DEFAULT now(),
  UpdatedAt     timestamptz DEFAULT now()
)
```
> Note: no `PasswordHash` column — Supabase Auth owns credentials. This table mirrors profile data only.

### Properties
```sql
Properties (
  Id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  LandlordId    uuid NOT NULL REFERENCES Users(Id),
  Name          varchar NOT NULL,
  Address       varchar NOT NULL,
  City          varchar NOT NULL,
  Province      varchar NOT NULL,
  ZipCode       varchar NOT NULL,
  Description   varchar,
  MonthlyRent   decimal(12,2) NOT NULL,
  FurnishingType varchar NOT NULL,         -- 'Bare' | 'SemiFurnished' | 'FullyFurnished'
  RequiresDeposit boolean DEFAULT false,
  DepositAmount decimal(12,2) DEFAULT 0,
  RequiresAdvance boolean DEFAULT false,
  AdvanceMonths int DEFAULT 0,
  CreatedAt     timestamptz DEFAULT now(),
  UpdatedAt     timestamptz DEFAULT now()
)
-- Index: LandlordId
```

### PropertyInventory
```sql
PropertyInventory (
  Id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  PropertyId    uuid NOT NULL REFERENCES Properties(Id) ON DELETE CASCADE,
  ItemName      varchar NOT NULL,
  Quantity      int DEFAULT 1,
  Condition     varchar,
  Notes         varchar,
  CreatedAt     timestamptz DEFAULT now()
)
-- Index: PropertyId
```

### Leases
```sql
Leases (
  Id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  PropertyId     uuid NOT NULL REFERENCES Properties(Id),
  TenantId       uuid NOT NULL REFERENCES Users(Id),
  StartDate      date NOT NULL,
  EndDate        date NOT NULL,
  MonthlyRent    decimal(12,2) NOT NULL,
  DepositTaken   boolean DEFAULT false,
  DepositAmount  decimal(12,2) DEFAULT 0,
  AdvanceTaken   boolean DEFAULT false,
  AdvanceMonths  int DEFAULT 0,
  Status         varchar NOT NULL DEFAULT 'Active',  -- 'Active' | 'Expired' | 'Terminated'
  CreatedAt      timestamptz DEFAULT now(),
  UpdatedAt      timestamptz DEFAULT now()
)
-- Index: PropertyId, TenantId, Status
```

### ReminderSettings
```sql
ReminderSettings (
  Id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  LeaseId       uuid NOT NULL REFERENCES Leases(Id) ON DELETE CASCADE,
  DaysBefore    int DEFAULT 3,
  DaysAfter     int DEFAULT 3,
  IsEnabled     boolean DEFAULT true
)
-- Unique index: LeaseId (one setting per lease)
```

### Payments
```sql
Payments (
  Id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  LeaseId         uuid NOT NULL REFERENCES Leases(Id),
  DueDate         date NOT NULL,
  AmountDue       decimal(12,2) NOT NULL,
  AmountPaid      decimal(12,2) DEFAULT 0,
  Status          varchar NOT NULL DEFAULT 'Unpaid',  -- 'Unpaid' | 'Partial' | 'Paid'
  StripeSessionId varchar,
  ProofFileUrl    varchar,
  Notes           varchar,
  PaidAt          timestamptz,
  CreatedAt       timestamptz DEFAULT now(),
  UpdatedAt       timestamptz DEFAULT now()
)
-- Index: LeaseId, DueDate, Status
```

### MaintenanceRequests
```sql
MaintenanceRequests (
  Id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  PropertyId      uuid NOT NULL REFERENCES Properties(Id),
  TenantId        uuid NOT NULL REFERENCES Users(Id),
  Title           varchar NOT NULL,
  Description     varchar NOT NULL,
  Status          varchar NOT NULL DEFAULT 'Open',   -- 'Open' | 'InProgress' | 'Resolved' | 'Closed'
  Priority        varchar NOT NULL DEFAULT 'Medium', -- 'Low' | 'Medium' | 'High' | 'Urgent'
  ImageUrl        varchar,
  ResolutionNotes varchar,
  ResolvedAt      timestamptz,
  CreatedAt       timestamptz DEFAULT now(),
  UpdatedAt       timestamptz DEFAULT now()
)
-- Index: PropertyId, TenantId, Status
```

### LeaseDocuments
```sql
LeaseDocuments (
  Id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  LeaseId       uuid NOT NULL REFERENCES Leases(Id),
  PaymentId     uuid REFERENCES Payments(Id),        -- null for lease agreements
  Type          varchar NOT NULL,                    -- 'LeaseAgreement' | 'Receipt'
  FileUrl       varchar NOT NULL,
  GeneratedAt   timestamptz DEFAULT now()
)
-- Index: LeaseId, Type
```

### AuditLogs
```sql
AuditLogs (
  Id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  ActorUserId   uuid NOT NULL REFERENCES Users(Id),
  EntityType    varchar NOT NULL,   -- 'Property' | 'Lease' | 'Payment' | 'Maintenance' | etc.
  EntityId      varchar NOT NULL,   -- Guid as string (allows deleted-entity references)
  Action        varchar NOT NULL,   -- 'created' | 'updated' | 'deleted' | 'resolved' | etc.
  PayloadJson   varchar,            -- JSON snapshot of relevant fields for display
  CreatedAt     timestamptz DEFAULT now()
)
-- Index: ActorUserId, EntityType, CreatedAt
```

### PaymentWebhookEvents
```sql
PaymentWebhookEvents (
  Id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  StripeEventId varchar UNIQUE NOT NULL,
  EventType     varchar NOT NULL,
  RawPayload    text NOT NULL,
  ProcessedAt   timestamptz DEFAULT now()
)
-- Unique index: StripeEventId (idempotency)
```

---

## PDF Generation Flow (Wave 4)

```
Landlord clicks "Generate Lease Agreement"
  → POST /api/leases/{id}/generate-agreement
  → DocumentService.GenerateLeaseAgreementAsync(leaseId)
      → Load lease + property + tenant from DB
      → Build QuestPDF document (LeaseAgreementDocument.cs)
      → Render to byte[]
      → SupabaseStorageService.UploadAsync(bytes, "lease-documents", fileName)
      → Save LeaseDocument record to DB (type = LeaseAgreement, fileUrl)
      → Write AuditLog entry
  ← Return { fileUrl, generatedAt }
  → Angular opens fileUrl in new tab (signed URL)

Payment confirmed (Stripe webhook or manual):
  → PaymentService.MarkAsPaidAsync(paymentId)
      → Update Payment status = Paid
      → DocumentService.GenerateReceiptAsync(paymentId)  (same flow as above)
      → Write AuditLog entry
```

**Library:** `QuestPDF` (NuGet package `QuestPDF`, MIT license, free for revenue < $1M/year).

---

## Authentication Flow

1. Landlord registers via Supabase Auth (Angular calls `supabase.auth.signUp()`)
2. Backend `POST /api/auth/register` mirrors the user profile to the `Users` table
3. Supabase issues a signed JWT (RS256); Angular stores it via `supabase.auth.getSession()`
4. JWT interceptor attaches `Authorization: Bearer <token>` to all API calls
5. .NET middleware validates JWT using Supabase public JWKS URL (cached)
6. Claims extracted: `sub` (= User.Id), `email`, role from `app_metadata`

**Tenant invite flow:**
1. Landlord calls `POST /api/auth/invite-tenant` with tenant email
2. Backend generates a secure token, stores it with expiry, sends invite email via SendGrid
3. Tenant clicks link → lands on `/accept-invite?token=...` page in Angular
4. Angular calls Supabase `supabase.auth.signUp()` with the invite token
5. Backend `POST /api/auth/accept-invite` mirrors profile and links tenant to landlord account
6. Tenant redirected to their dashboard

---

## Supabase Storage Buckets

| Bucket | Contents | Access |
|--------|---------|--------|
| `tenant-ids` | Tenant ID photos and PDFs | Private — signed URLs, landlord + that tenant only |
| `payment-proofs` | Manual payment screenshots | Private — signed URLs, landlord + that tenant only |
| `maintenance-images` | Maintenance photos | Private — signed URLs, landlord + that tenant only |
| `lease-documents` | Lease agreement and receipt PDFs | Private — signed URLs, landlord + that tenant only |

---

## External Integrations

| Integration | Purpose | Key |
|-------------|---------|-----|
| Supabase Auth | Registration, login, JWT issuance, invite | `SUPABASE_SERVICE_ROLE_KEY` (server only) |
| Supabase Storage | File upload + signed URL generation | `SUPABASE_SERVICE_ROLE_KEY` |
| Stripe Checkout | Online rent payments | `STRIPE_SECRET_KEY`, `STRIPE_WEBHOOK_SECRET` |
| SendGrid | Email reminders, invite emails, welcome emails | `SENDGRID_API_KEY` |
| QuestPDF | Lease agreement + receipt PDF generation | MIT — no API key needed |

---

## Deployment Topology

| Component | Platform |
|-----------|---------|
| Angular SPA | Vercel (auto-deploy from `main`) |
| .NET 8 API | Railway (or Render / Fly.io) |
| PostgreSQL | Supabase (free tier) |
| Auth + Storage | Supabase |
| Daily cron | GitHub Actions scheduled workflow |

All secrets in environment variables. Nothing in source control.
