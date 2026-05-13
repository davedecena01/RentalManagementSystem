# Backfill Features Design: Audit Log, Onboarding Wizard, Demo Seed Data

**Date:** 2026-05-14
**Status:** Approved
**Scope:** Wave 3/4 backfill — 3 features

---

## Overview

Three features to complete the MVP backfill:

1. **Structured Audit Log / Activity Feed** — log major entity mutations; surfaced on dashboard and a dedicated page
2. **Landlord Onboarding Wizard** — 4-step + tour overlay for first-time landlords
3. **Demo Seed Data** — backend seed endpoint + UI "Load Demo Data" button

SendGrid invite email is deferred (SendGrid not yet configured).

---

## Feature 1: Audit Log / Activity Feed

### Backend

**New entity: `AppLog`**

```
Id           Guid (PK)
UserId       Guid (FK → Users)
Action       string  — e.g. "property.created", "lease.updated"
EntityType   string  — "Property" | "Lease" | "Payment" | "Maintenance" | "Tenant"
EntityId     Guid?
Description  string  — human-readable summary
CreatedAt    DateTime (UTC)
```

Index on `(UserId, CreatedAt DESC)` for efficient feed queries.

**New service: `AppLogService`**

Single method:
```csharp
Task LogAsync(Guid userId, string action, string entityType, Guid? entityId, string description);
```

Injected into `PropertyService`, `LeaseService`, `PaymentService`, `MaintenanceService`.

**Actions logged:**

| Action | Trigger |
|--------|---------|
| `property.created` | Property created |
| `property.updated` | Property updated |
| `lease.created` | Lease created |
| `lease.updated` | Lease status changed |
| `payment.recorded` | Manual payment recorded |
| `payment.stripe_paid` | Stripe webhook confirms payment |
| `maintenance.submitted` | Maintenance request created |
| `maintenance.resolved` | Maintenance request resolved |
| `tenant.invited` | Tenant invite sent |

**New endpoint: `GET /api/logs`**

- Requires `[Authorize(Roles = "Landlord")]`
- Scoped to calling landlord's userId
- Query params: `entityType` (optional filter), `page`, `pageSize` (default 20)
- Response: `{ items: AppLogDto[], totalCount, page, pageSize }`

**DTO: `AppLogDto`**
```
id, userId, action, entityType, entityId, description, createdAt
```

**Migration:** add `AppLogs` table.

### Frontend

**Dashboard card: "Recent Activity"**
- Shows last 5 log entries
- Each entry: icon by entity type, description, relative timestamp (e.g. "2 hours ago")
- "View all activity →" link to `/account/activity`

**Dedicated page: `/account/activity`**
- Route added to `accountRoutes`
- Filter bar: All / Properties / Leases / Payments / Maintenance
- Paginated scrollable list (load more button)
- Empty state: "No activity yet."
- Loading skeleton while fetching

**New `ApiService` method:** `getLogs(entityType?, page?, pageSize?)`

---

## Feature 2: Landlord Onboarding Wizard

### Trigger

- Shown automatically on dashboard load for landlords where `user.onboardingCompleted === false`
- Current step persisted in `localStorage` key `onboarding_step` so refresh resumes progress
- "Skip for now" hides wizard for the session (does NOT set `onboardingCompleted`)
- Only step 4 completion permanently dismisses it

### 4 Steps

**Step 1 — Welcome + Profile**
- Greet landlord: "Welcome to Rental Property Manager, {firstName}!"
- Pre-fill name and phone from current user profile
- Inline form: firstName, lastName, phone (all editable)
- "Save & Continue" → calls `PATCH /api/users/me`, advances to step 2

**Step 2 — Add Your First Property**
- Brief copy: "Properties are the foundation of your portfolio."
- "Create Property" button → navigates to `/properties/new` (wizard modal closes, step saved)
- On return to dashboard: "I've added my property →" button to advance to step 3
- Alternatively, if `properties.length > 0`, auto-show "I've added my property" as active

**Step 3 — Create Your First Lease**
- Brief copy: "Assign a tenant to your property and schedule rent payments."
- "Create Lease" button → navigates to `/leases/new`
- Same "I've done this →" advance pattern

**Step 4 — Explore the Dashboard**
- Tooltip tour highlighting 3 elements:
  1. Portfolio summary card ("Your properties at a glance")
  2. Unpaid rent card ("Track outstanding payments")
  3. Open maintenance card ("Monitor active requests")
- "Finish Tour" button → calls `PATCH /api/users/me` with `{ onboardingCompleted: true }`, clears `localStorage`, dismisses wizard permanently

### Component Design

`OnboardingWizardComponent` — standalone, rendered inside `DashboardComponent` via `@if (!user.onboardingCompleted && !sessionSkipped)`

Full-screen overlay with centered card:
- Step indicator: "Step 2 of 4"
- Step content (dynamic per step)
- "Skip for now" link (bottom-left)
- Primary CTA button (bottom-right)

Tooltip tour (step 4): lightweight CSS-positioned tooltip divs, not a third-party library.

### Backend change

No new endpoints needed. Uses existing `PATCH /api/users/me` which already accepts profile fields. Add `onboardingCompleted` to the `UpdateMeRequest` DTO and handler.

---

## Feature 3: Demo Seed Data

### Backend: `POST /api/seed/demo`

- Requires `[Authorize(Roles = "Landlord")]`
- **Idempotent:** if a property named `"Sunset Apartments Unit 1"` already exists for this landlord, return `200 { message: "Demo data already loaded" }`
- Creates everything scoped to the calling landlord:

| Entity | Details |
|--------|---------|
| Properties | "Sunset Apartments Unit 1" (₱15,000/mo), "Greenview Townhouse" (₱22,000/mo) |
| Tenant users | `tenant1@demo.com` / `Tenant1Demo!`, `tenant2@demo.com` / `Tenant2Demo!` — role: Tenant |
| Leases | One per property, Active, start 6 months ago, end 6 months from now |
| Payments | 6 total — 2 Paid, 2 Partial, 2 Unpaid — spread across leases |
| Maintenance | 3 requests — 1 Open, 1 InProgress, 1 Resolved |
| Activity logs | Log entries for all created entities (property.created, lease.created, etc.) |

- Returns: `{ message: "Demo data loaded", summary: { properties: 2, tenants: 2, leases: 2, payments: 6, maintenanceRequests: 3 } }`

**New controller:** `SeedController` — thin, delegates to `SeedService`.

### Frontend

**Dashboard "Load Demo Data" banner/button:**
- Shown only to landlords where `properties.length === 0`
- Renders as an info banner: "No properties yet. Want to explore with sample data? [Load Demo Data]"
- Clicking shows confirmation dialog: "This will create sample properties, tenants, leases, and payments for demo purposes. Continue?"
- On confirm: calls `POST /api/seed/demo`, shows success toast "Demo data loaded! Refreshing…", then reloads dashboard data
- Banner disappears once properties exist (condition-driven)

**New `ApiService` method:** `loadDemoData()`

---

## Implementation Order

1. Audit log backend (entity, service, migration, controller)
2. Wire `AppLogService` into existing services
3. Audit log frontend (dashboard card + `/account/activity` page)
4. Onboarding wizard component + dashboard integration
5. `onboardingCompleted` field added to `UpdateMeRequest` DTO
6. Demo seed backend (SeedService + SeedController)
7. Demo seed frontend (dashboard banner + confirmation dialog)

---

## Out of Scope

- SendGrid invite email (deferred — no SendGrid configured)
- SMS notifications
- Admin-level cross-landlord log visibility
- Exporting logs to CSV
- Complex onboarding analytics
