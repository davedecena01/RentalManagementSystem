# Lease Special Provisions Design

**Date:** 2026-05-11
**Feature:** Wave 5 — Lease Special Provisions (Custom Contract Clauses)
**Branch:** `feature/wave-5-lease-special-provisions`

---

## Goal

Allow landlords to enrich lease agreements with reusable and one-off special provisions (custom contract clauses) that appear as a dedicated section in the generated lease PDF — matching the structure of real-world rental contracts.

---

## Architecture

Two-layer system: a per-landlord **Provision Template Library** for reuse across leases, plus per-lease **Lease Provisions** that snapshot template content at time of assignment. Provisions render as a "Special Provisions" section at the bottom of the QuestPDF lease agreement, before the signature block.

**Tech stack:** C# / .NET 8, Entity Framework Core, QuestPDF, Angular, PostgreSQL (Supabase).

---

## Data Model

### `ProvisionTemplates` table

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `Guid` | PK |
| `LandlordId` | `Guid` | FK → Users; row-level landlord scoping |
| `Title` | `string(200)` | e.g. "No Pets Policy" |
| `Body` | `string(4000)` | clause text |
| `CreatedAt` | `DateTime` | UTC |

### `LeaseProvisions` table

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `Guid` | PK |
| `LeaseId` | `Guid` | FK → Leases (cascade delete) |
| `Title` | `string(200)` | snapshot of title at assignment time |
| `Body` | `string(4000)` | snapshot of body at assignment time |
| `SortOrder` | `int` | controls PDF ordering |
| `CreatedAt` | `DateTime` | UTC |

**Snapshot pattern:** when a landlord selects a template for a lease, the title and body are copied into `LeaseProvisions` — not referenced live. Editing the template later does not retroactively change existing lease PDFs.

---

## Backend API

### Provision Templates — `/api/provision-templates`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/api/provision-templates` | Landlord | List landlord's templates |
| `POST` | `/api/provision-templates` | Landlord | Create template `{ title, body }` |
| `PUT` | `/api/provision-templates/{id}` | Landlord | Update template (does not affect existing lease provisions) |
| `DELETE` | `/api/provision-templates/{id}` | Landlord | Delete template (does not affect existing lease provisions) |

Authorization: every query filters by `LandlordId == currentUser.Id`.

### Lease Provisions — `/api/leases/{id}/provisions`

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/api/leases/{id}/provisions` | Landlord or Tenant (own lease) | List provisions for a lease |
| `POST` | `/api/leases/{id}/provisions` | Landlord | Add provisions: `[{ title, body, sortOrder }]` |
| `PUT` | `/api/leases/{id}/provisions` | Landlord | Full replace (reorder + edit): `[{ id?, title, body, sortOrder }]` |
| `DELETE` | `/api/leases/{id}/provisions/{provisionId}` | Landlord | Remove one provision |

Authorization: landlord must own the lease; tenant must be the lease's assigned tenant.

### PDF (`GET /api/leases/{id}/pdf`) — no new endpoint

`PdfService` will eager-load `LeaseProvisions` ordered by `SortOrder`. If provisions exist, a "Special Provisions" section is appended to the PDF before the signature block.

---

## PDF Layout

```
┌────────────────────────────────────────┐
│  LEASE AGREEMENT                       │
│  [Property / Parties / Lease Terms]    │
│                                        │
│  SPECIAL PROVISIONS                    │  ← rendered only if provisions exist
│  ──────────────────────────────────    │
│  No Pets Policy                        │  ← bold title
│  Tenant agrees that no animals of      │  ← body text (normal weight)
│  any kind shall be kept on premises.   │
│                                        │
│  Early Termination Fee                 │  ← next provision (SortOrder)
│  If Tenant terminates lease before...  │
│                                        │
│  ────────────────────────────────────  │
│  [Signature Block]                     │
└────────────────────────────────────────┘
```

---

## Frontend Components

### New: Account > Provision Templates page

- Route: `/account/provision-templates`
- Inline table: Title, Body (truncated preview), Edit inline, Delete with confirmation
- "Add Template" form above the table (Title + Body textarea, Save button)
- Navigation link added under the Account section

### Modified: Lease Form (`/leases/new`)

New "Special Provisions" section at bottom of the form (above the submit button):
1. **Library picker** — checklist of landlord's saved templates; checking one adds it to the list
2. **One-off provisions** — "Add provision" button reveals title + body inputs; multiple can be added
3. On form submit: lease is created first, then provisions are posted to `/api/leases/{id}/provisions`

### Modified: Lease Detail (`/leases/:id`)

New "Special Provisions" card below the lease detail grid:
- **Landlord view**: editable list — add, remove, reorder (up/down arrows), edit title/body inline; "Save" persists via PUT
- **Tenant view**: read-only ordered list (title bold, body text); shown only if provisions exist

### Modified: `ApiService`

New methods:
```typescript
// Provision templates
getProvisionTemplates(): Observable<ProvisionTemplate[]>
createProvisionTemplate(payload: { title: string; body: string }): Observable<ProvisionTemplate>
updateProvisionTemplate(id: string, payload: { title: string; body: string }): Observable<ProvisionTemplate>
deleteProvisionTemplate(id: string): Observable<void>

// Lease provisions
getLeaseProvisions(leaseId: string): Observable<LeaseProvision[]>
setLeaseProvisions(leaseId: string, provisions: LeaseProvisionPayload[]): Observable<LeaseProvision[]>
deleteLeaseProvision(leaseId: string, provisionId: string): Observable<void>
```

---

## Authorization Rules

- Only landlords can create, update, or delete provision templates.
- Only the landlord who owns a lease can create, update, or delete its provisions.
- Both landlord (owner) and tenant (assigned) can read lease provisions.
- Backend must enforce all authorization checks — frontend role checks are UX only.

---

## Error Handling

- 404 if template or provision not found
- 403 if landlord tries to access another landlord's template or lease
- 403 if tenant tries to modify provisions
- 400 if title or body is empty
- Consistent `{ error, code }` JSON shape per project conventions

---

## Out of Scope

- Rich text / HTML in provisions (plain text only for MVP)
- Provision versioning or change history
- Sharing provision templates across landlord accounts
- Reordering provisions via drag-and-drop (up/down buttons suffice for MVP)
- E-signature integration
