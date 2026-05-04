# Changelog — Rental Property Manager

All notable changes to this project will be documented in this file.

Format: `## [date] — Brief title`
Newest entries first.

---

## [2026-05-05] — Spec Refinement and MVP Wave Restructure

### Changed
- `docs/product/project-spec.md` — major rewrite:
  - Locked Angular as the only frontend (removed Next.js/React references)
  - Hard-coded PHP (₱) currency (removed per-property currency field)
  - Tenant onboarding changed to landlord-invite-only (removed self-register path)
  - Switched primary key type from `int` to `Guid`
  - Removed `passwordHash` from `Users` (Supabase Auth owns credentials)
  - Moved `tenantName`/`tenantContact` off `Lease` → use FK to `Users`
  - Moved `reminder_before_days`/`reminder_after_days` off `Lease` → `ReminderSetting` table
  - Fixed inventory DELETE endpoint nesting under property
  - Added `AuditLog` and `LeaseDocument` entities to data model
  - Restructured sprint plan into 4 waves (6–8 weeks)
  - Added "Differentiator Features" section (PDF generation, audit log, onboarding wizard)
  - Added "Backlog / Out of Scope" section
  - Added "Resolved Decisions" table
- `docs/technical/system-architecture.md` — updated:
  - Added `AuditLog` and `LeaseDocument` DB schema sections
  - Added PDF generation flow (QuestPDF library)
  - Added `lease-documents` Supabase Storage bucket
  - Removed `passwordHash` note; clarified profile mirror pattern
  - Added Angular folder for `audit-log` feature and `accept-invite` page
  - Added `DocumentService` and audit integration folder
- `docs/technical/api-contracts.md` — updated:
  - Added `POST /api/auth/invite-tenant` and `POST /api/auth/accept-invite`
  - Added `POST /api/leases/{id}/generate-agreement`
  - Added `POST /api/payments/{id}/generate-receipt`
  - Added `GET /api/leases/{id}/documents`
  - Added `GET /api/audit-log`
  - Added `PDF_GENERATION_ERROR` error code
  - Updated all currency examples to PHP (₱)
  - Clarified Stripe webhook signature validation (no JWT)
- `docs/technical/deployment.md` — updated:
  - Added `PDF_STORAGE_BUCKET` and `DEMO_MODE` environment variables
  - Added seed data specification (demo landlord, tenants, properties, payments)
  - Added `/demo` public page description
  - Added `lease-documents` bucket to Supabase Storage setup table
- `docs/planning/execution-plan.md` — replaced Phase 0–13 structure with 4-wave structure:
  - Each wave has backend + frontend + infrastructure checklists
  - Each wave has explicit demo acceptance criteria
  - Backlog section added for post-MVP features
- `docs/project-status.md` — updated:
  - All decisions from this session added to Decisions Made table
  - Phase table replaced with Wave structure
  - Open Questions refined to 3 unresolved items
- `.env.example` — added `PDF_STORAGE_BUCKET` and `DEMO_MODE` variables

### Context
Research into competing rental management SaaS (Buildium, Stessa, AppFolio, Innago, RentRedi) confirmed the original 3-week spec covered table-stakes features but lacked the polish layer that differentiates premium tools. This refinement adds three targeted differentiators (PDF generation, audit log, onboarding wizard) within a 6–8-week timeline and locks all previously open technical decisions.

---

## [2026-05-05] — Project Documentation Scaffolded (Initial)

### Added
- `docs/product/project-spec.md` — initial product specification
- `docs/technical/system-architecture.md` — architecture overview
- `docs/technical/api-contracts.md` — REST API contracts
- `docs/technical/deployment.md` — deployment guide
- `docs/planning/execution-plan.md` — phased implementation plan
- `docs/project-status.md` — living status tracker
- `docs/changelog.md` — this file
- `.env.example` — sample environment variable file

---

_Future entries go above this line, newest first._
