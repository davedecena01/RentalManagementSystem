# API Contracts — Rental Property Manager

## Conventions

- Base URL: `https://<api-host>/api`
- Protected endpoints require: `Authorization: Bearer <supabase-jwt>`
- All bodies: `application/json`
- Currency: **PHP (₱)** — money fields are `number` with 2 decimal places
- Dates: `YYYY-MM-DD` (date-only), `YYYY-MM-DDTHH:mm:ssZ` (timestamps)
- IDs: UUID strings
- Pagination: `?page=1&pageSize=20`
- Error shape (all non-2xx):
  ```json
  { "error": "Human-readable message", "code": "ERROR_CODE" }
  ```

---

## Auth — `/api/auth`

### POST `/api/auth/register`
Creates the backend `User` profile after Supabase Auth registration. Called by Angular immediately after `supabase.auth.signUp()` succeeds.

**Request:**
```json
{
  "email": "landlord@example.com",
  "fullName": "Juan Dela Cruz",
  "role": "Landlord"
}
```
**Response `201`:**
```json
{
  "id": "uuid",
  "email": "landlord@example.com",
  "fullName": "Juan Dela Cruz",
  "role": "Landlord",
  "onboardingCompleted": false,
  "createdAt": "2026-01-01T00:00:00Z"
}
```

---

### POST `/api/auth/invite-tenant`
Landlord invites a tenant by email. Sends a SendGrid invite email with a secure token link.

**Auth:** Landlord only.

**Request:**
```json
{
  "email": "tenant@example.com",
  "fullName": "Maria Cruz"
}
```
**Response `200`:**
```json
{
  "message": "Invite sent to tenant@example.com"
}
```

---

### POST `/api/auth/accept-invite`
Tenant accepts the invite after setting their password via Supabase Auth. Mirrors the tenant profile to the backend `Users` table.

**No auth header required** — uses the Supabase JWT from `signUp()` completion.

**Request:**
```json
{
  "email": "tenant@example.com",
  "fullName": "Maria Cruz"
}
```
**Response `201`:**
```json
{
  "id": "uuid",
  "email": "tenant@example.com",
  "fullName": "Maria Cruz",
  "role": "Tenant",
  "createdAt": "2026-01-01T00:00:00Z"
}
```

---

## Users — `/api/users`

### GET `/api/users/me`
**Response `200`:**
```json
{
  "id": "uuid",
  "email": "user@example.com",
  "fullName": "Juan Dela Cruz",
  "role": "Landlord",
  "phone": "+639171234567",
  "onboardingCompleted": false,
  "tenantIdUrl": null,
  "createdAt": "2026-01-01T00:00:00Z"
}
```

### PATCH `/api/users/me`
**Request:**
```json
{
  "fullName": "Juan Dela Cruz",
  "phone": "+639171234567",
  "onboardingCompleted": true
}
```
**Response `200`:** Updated user object (same shape as GET).

---

## Properties — `/api/properties`

### GET `/api/properties`
Landlord's own properties only. Optionally includes `isOccupied` (derived from active lease).

**Query:** `?page=1&pageSize=20&search=`

**Response `200`:**
```json
{
  "data": [
    {
      "id": "uuid",
      "name": "Sunset Studio 1A",
      "address": "123 Mango St",
      "city": "Cebu City",
      "province": "Cebu",
      "zipCode": "6000",
      "monthlyRent": 8000.00,
      "furnishingType": "SemiFurnished",
      "requiresDeposit": true,
      "depositAmount": 16000.00,
      "requiresAdvance": true,
      "advanceMonths": 1,
      "isOccupied": true,
      "createdAt": "2026-01-01T00:00:00Z"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20
}
```

### POST `/api/properties`
**Request:**
```json
{
  "name": "Sunset Studio 1A",
  "address": "123 Mango St",
  "city": "Cebu City",
  "province": "Cebu",
  "zipCode": "6000",
  "description": "Studio unit on 2nd floor",
  "monthlyRent": 8000.00,
  "furnishingType": "SemiFurnished",
  "requiresDeposit": true,
  "depositAmount": 16000.00,
  "requiresAdvance": true,
  "advanceMonths": 1
}
```
**Response `201`:** Full property object.

### GET `/api/properties/{id}`
**Response `200`:** Full property object.

### PUT `/api/properties/{id}`
**Request:** Same as POST. **Response `200`:** Updated property.

### DELETE `/api/properties/{id}`
Returns `409` if property has an active lease.
**Response `204`:** No content.

---

## Property Inventory — `/api/properties/{id}/inventory`

### GET `/api/properties/{id}/inventory`
**Response `200`:**
```json
[
  {
    "id": "uuid",
    "propertyId": "uuid",
    "itemName": "Air Conditioner",
    "quantity": 1,
    "condition": "Good",
    "notes": "Inverter 1.5HP"
  }
]
```

### POST `/api/properties/{id}/inventory`
**Request:**
```json
{
  "itemName": "Air Conditioner",
  "quantity": 1,
  "condition": "Good",
  "notes": "Inverter 1.5HP"
}
```
**Response `201`:** Inventory item object.

### PUT `/api/properties/{propertyId}/inventory/{itemId}`
**Request:** Same as POST. **Response `200`:** Updated item.

### DELETE `/api/properties/{propertyId}/inventory/{itemId}`
**Response `204`:** No content.

---

## Leases — `/api/leases`

### GET `/api/leases`
Landlord's leases across all properties.

**Query:** `?page=1&pageSize=20&propertyId=&status=Active`

**Response `200`:**
```json
{
  "data": [
    {
      "id": "uuid",
      "propertyId": "uuid",
      "propertyName": "Sunset Studio 1A",
      "tenantId": "uuid",
      "tenantName": "Maria Cruz",
      "tenantEmail": "maria@example.com",
      "startDate": "2026-01-01",
      "endDate": "2026-12-31",
      "monthlyRent": 8000.00,
      "depositTaken": true,
      "depositAmount": 16000.00,
      "advanceTaken": true,
      "advanceMonths": 1,
      "status": "Active",
      "createdAt": "2026-01-01T00:00:00Z"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20
}
```

### POST `/api/leases`
Creates lease and auto-generates monthly `Payment` records.

**Request:**
```json
{
  "propertyId": "uuid",
  "tenantId": "uuid",
  "startDate": "2026-01-01",
  "endDate": "2026-12-31",
  "monthlyRent": 8000.00,
  "depositTaken": true,
  "depositAmount": 16000.00,
  "advanceTaken": true,
  "advanceMonths": 1
}
```
**Response `201`:** Lease object.

### GET `/api/leases/{id}`
**Response `200`:** Lease object with tenant and property summary.

### PUT `/api/leases/{id}`
**Response `200`:** Updated lease object.

### PATCH `/api/leases/{id}/status`
**Request:**
```json
{ "status": "Terminated" }
```
**Response `200`:** Updated lease object.

### GET `/api/leases/my` _(Tenant only)_
**Response `200`:** Single lease object for the authenticated tenant's current active lease.

---

## Lease Documents — `/api/leases/{id}/documents`

### POST `/api/leases/{id}/generate-agreement` _(Landlord only)_
Generates a lease agreement PDF using QuestPDF, saves to Supabase Storage.

**Response `200`:**
```json
{
  "id": "uuid",
  "leaseId": "uuid",
  "type": "LeaseAgreement",
  "fileUrl": "https://supabase-storage.../lease-agreement.pdf",
  "generatedAt": "2026-01-01T08:00:00Z"
}
```

### POST `/api/payments/{id}/generate-receipt` _(Landlord or auto-triggered)_
Generates a rent receipt PDF for a Paid payment. Also auto-triggered by webhook and manual payment confirmation.

**Response `200`:**
```json
{
  "id": "uuid",
  "leaseId": "uuid",
  "paymentId": "uuid",
  "type": "Receipt",
  "fileUrl": "https://supabase-storage.../receipt-jan-2026.pdf",
  "generatedAt": "2026-01-01T08:00:00Z"
}
```

### GET `/api/leases/{id}/documents`
All documents (agreements + receipts) for a lease.

**Response `200`:**
```json
[
  {
    "id": "uuid",
    "type": "LeaseAgreement",
    "fileUrl": "https://...",
    "generatedAt": "2026-01-01T08:00:00Z"
  },
  {
    "id": "uuid",
    "type": "Receipt",
    "paymentId": "uuid",
    "fileUrl": "https://...",
    "generatedAt": "2026-02-01T08:00:00Z"
  }
]
```

---

## Payments — `/api/payments`

### GET `/api/payments`
Landlord-scoped payment list.

**Query:** `?leaseId=&status=Unpaid&page=1&pageSize=20`

**Response `200`:**
```json
{
  "data": [
    {
      "id": "uuid",
      "leaseId": "uuid",
      "dueDate": "2026-02-01",
      "amountDue": 8000.00,
      "amountPaid": 0.00,
      "status": "Unpaid",
      "paidAt": null,
      "proofFileUrl": null,
      "stripeSessionId": null,
      "receiptUrl": null,
      "notes": null
    }
  ],
  "totalCount": 12,
  "page": 1,
  "pageSize": 20
}
```

### GET `/api/payments/{id}`
**Response `200`:** Single payment object.

### PATCH `/api/payments/{id}/manual` _(Landlord)_
Records a manual payment.

**Request:**
```json
{
  "amountPaid": 8000.00,
  "notes": "Cash handed over",
  "proofFileUrl": "https://supabase-storage.../proof.jpg"
}
```
**Response `200`:** Updated payment. Auto-triggers receipt generation if `amountPaid >= amountDue`.

### GET `/api/payments/my` _(Tenant only)_
**Response `200`:** Paginated payment list for tenant's active lease.

---

## Stripe — `/api/payments/stripe`

### POST `/api/payments/stripe/checkout-session` _(Tenant only)_
Creates a Stripe Checkout Session.

**Request:**
```json
{ "paymentId": "uuid" }
```
**Response `200`:**
```json
{ "checkoutUrl": "https://checkout.stripe.com/pay/cs_..." }
```

### POST `/api/payments/stripe/webhook`
Stripe webhook endpoint.
- **No** `Authorization` header.
- Validates `Stripe-Signature` header against `STRIPE_WEBHOOK_SECRET`.
- Handles: `checkout.session.completed`
- Idempotency: checks `PaymentWebhookEvents` for duplicate `StripeEventId` before processing.

**Response `200`:** Acknowledgement (always — Stripe needs 200 to stop retrying).

---

## Maintenance — `/api/maintenance`

### GET `/api/maintenance`
Landlord view: all requests across landlord's properties.

**Query:** `?propertyId=&status=Open&page=1&pageSize=20`

**Response `200`:**
```json
{
  "data": [
    {
      "id": "uuid",
      "propertyId": "uuid",
      "propertyName": "Sunset Studio 1A",
      "tenantId": "uuid",
      "tenantName": "Maria Cruz",
      "title": "Leaking faucet",
      "description": "Kitchen faucet leaks when turned off",
      "status": "Open",
      "priority": "Medium",
      "imageUrl": null,
      "resolutionNotes": null,
      "resolvedAt": null,
      "createdAt": "2026-02-01T08:00:00Z"
    }
  ],
  "totalCount": 3,
  "page": 1,
  "pageSize": 20
}
```

### POST `/api/maintenance` _(Tenant only)_
**Request:**
```json
{
  "propertyId": "uuid",
  "title": "Leaking faucet",
  "description": "Kitchen faucet drips constantly",
  "priority": "Medium",
  "imageUrl": null
}
```
**Response `201`:** Maintenance request object.

### GET `/api/maintenance/{id}`
**Response `200`:** Full request object.

### PATCH `/api/maintenance/{id}/status` _(Landlord only)_
**Request:**
```json
{
  "status": "Resolved",
  "resolutionNotes": "Replaced faucet washer"
}
```
**Response `200`:** Updated request object.

---

## Dashboard — `/api/dashboard`

### GET `/api/dashboard` _(Landlord only)_
**Response `200`:**
```json
{
  "totalProperties": 3,
  "occupiedProperties": 2,
  "vacantProperties": 1,
  "totalMonthlyRent": 24000.00,
  "totalUnpaidRent": 8000.00,
  "openMaintenanceRequests": 2,
  "monthlyIncome": [
    { "month": "2026-01", "totalPaid": 16000.00 },
    { "month": "2026-02", "totalPaid": 24000.00 }
  ],
  "paymentStatusBreakdown": {
    "paid": 4,
    "partial": 1,
    "unpaid": 1
  }
}
```

---

## Audit Log — `/api/audit-log`

### GET `/api/audit-log` _(Landlord only)_
Paginated, scoped to landlord's entities.

**Query:** `?page=1&pageSize=20&entityType=&dateFrom=&dateTo=`

**Response `200`:**
```json
{
  "data": [
    {
      "id": "uuid",
      "actorName": "Juan Dela Cruz",
      "entityType": "Payment",
      "entityId": "uuid",
      "action": "paid",
      "description": "Maria Cruz paid ₱8,000.00 for February 2026",
      "createdAt": "2026-02-01T10:30:00Z"
    }
  ],
  "totalCount": 48,
  "page": 1,
  "pageSize": 20
}
```

---

## Reminders — `/api/reminders`

### GET `/api/reminders` _(Landlord only)_
**Response `200`:** List of reminder settings, one per active lease.

### PUT `/api/reminders/{leaseId}` _(Landlord only)_
**Request:**
```json
{
  "daysBefore": 3,
  "daysAfter": 2,
  "isEnabled": true
}
```
**Response `200`:** Updated reminder setting.

### POST `/api/reminders/trigger` _(Internal — no JWT)_
Secured by `X-Internal-Key: <INTERNAL_API_KEY>` header. Called by GitHub Actions cron daily.

**Response `200`:**
```json
{
  "sent": 4,
  "skipped": 2,
  "errors": 0,
  "triggeredAt": "2026-02-01T02:00:00Z"
}
```

---

## File Uploads — `/api/files`

### POST `/api/files/upload`
Returns a signed Supabase Storage upload URL for direct client-to-Supabase upload.

**Request:**
```json
{
  "fileType": "payment-proof",
  "fileName": "january-receipt.jpg",
  "mimeType": "image/jpeg",
  "fileSizeBytes": 204800
}
```
Allowed `fileType` values: `tenant-id`, `payment-proof`, `maintenance-image`
Allowed MIME types: `image/jpeg`, `image/png`, `application/pdf`
Max file size: 10 MB (10,485,760 bytes)

**Response `200`:**
```json
{
  "uploadUrl": "https://supabase-storage.../signed-upload-url",
  "fileUrl": "https://supabase-storage.../final-path"
}
```

---

## Health Check

### GET `/healthz`
No auth required.

**Response `200`:**
```json
{
  "status": "healthy",
  "timestamp": "2026-02-01T08:00:00Z"
}
```

---

## HTTP Status Code Reference

| Code | Meaning |
|------|---------|
| `200` | Success |
| `201` | Created |
| `204` | No Content |
| `400` | Validation error |
| `401` | Missing or invalid JWT |
| `403` | Authenticated but not authorized (wrong role or wrong owner) |
| `404` | Resource not found |
| `409` | Conflict (e.g. delete blocked by active lease; duplicate Stripe event) |
| `422` | Unprocessable entity (business rule violation) |
| `500` | Internal server error |

## Error Code Reference

| Code | When Used |
|------|-----------|
| `VALIDATION_ERROR` | Request body failed validation |
| `UNAUTHENTICATED` | Missing or invalid JWT |
| `FORBIDDEN` | Wrong role or ownership violation |
| `NOT_FOUND` | Resource does not exist |
| `CONFLICT` | Delete blocked by dependency; duplicate webhook event |
| `BUSINESS_RULE_VIOLATION` | Lease date invalid, overlapping lease, etc. |
| `STRIPE_ERROR` | Stripe API error |
| `STORAGE_ERROR` | Supabase Storage failure |
| `PDF_GENERATION_ERROR` | QuestPDF rendering failed |
| `EMAIL_ERROR` | SendGrid send failed |
| `INTERNAL_ERROR` | Unexpected server error |
