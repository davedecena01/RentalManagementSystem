# Future Improvements

Tracked from the 2026-05-17 E2E + security sweep ([TEST_REPORT.md](TEST_REPORT.md), [SECURITY_FINDINGS.md](SECURITY_FINDINGS.md)). The two High-severity findings (F-005 / F-006) shipped in commit `9ba322c`. Everything below is open.

Ordered roughly by recommended sequence — cheap UX wins first, then prod-hardening, then nice-to-haves.

---

## 1. Surface backend errors in forms (F-003) — Medium UX
**Files:** `frontend/rental-management-ui/src/app/features/leases/lease-form/lease-form.component.ts`, `frontend/rental-management-ui/src/app/features/payments/payments-list/payments-list.component.ts`

The backend returns clean `{error, code}` for `INVALID_DATE_RANGE`, `TENANT_NOT_FOUND`, etc. The frontend currently swallows them — the user sees nothing when submit fails. The register/login forms already render the error correctly; the same pattern just needs to be wired into the lease form and the Stripe checkout button.

**Action:**
- Add a form-level error banner that subscribes to `HttpErrorResponse` from the relevant service call.
- Map `code` to a friendly message; fall back to `error` for unknown codes.
- Same fix covers F-007 (Stripe silent 500) once paired with #5.

**Effort:** ~30 min per page.

---

## 2. Cap `pageSize` server-side (F-008) — Low / DoS
**Files:** wherever `IQueryable<T>.Skip/Take` is wired to a query DTO. Likely `PropertiesController`, `LeasesController`, `PaymentsController`, `MaintenanceController`.

`?pageSize=99999` is currently accepted as-is. Fine for portfolio data; risky if the DB ever grows.

**Action:**
- Add a `[Range(1, 100)]` (or similar) attribute on the page-size property of each query DTO, OR
- Centralize via a `PagedQuery` base DTO with a clamped `PageSize` getter.

**Effort:** ~20 min.

---

## 3. Stop leaking .NET namespaces in validation errors (F-004) — Low / Info disclosure
**Symptom:** Default ASP.NET `ProblemDetails` for body-binding failures includes fully-qualified type names like `RentalManagementApi.DTOs.Provisions.LeaseProvisionPayload`.

**Action:** In `Program.cs`, register a custom `InvalidModelStateResponseFactory` (or a `ConfigureApiBehaviorOptions` callback) that returns the project's standard `ApiError` shape `{error, code}` — no framework type names.

```csharp
builder.Services.Configure<ApiBehaviorOptions>(opts =>
{
    opts.InvalidModelStateResponseFactory = ctx => new BadRequestObjectResult(
        new ApiError("Invalid request.", "VALIDATION_ERROR"));
});
```

**Effort:** ~10 min.

---

## 4. Add HSTS in production (F-009) — Info (prod-only)
**File:** `backend/RentalManagementApi/Middleware/SecurityHeadersMiddleware.cs` (or wherever security headers are set).

Other security headers (`X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy`, `Permissions-Policy`) are already correct. HSTS is missing.

**Action:**
```csharp
if (env.IsProduction())
{
    ctx.Response.Headers["Strict-Transport-Security"] =
        "max-age=31536000; includeSubDomains";
}
```

Gating to production keeps HTTP localhost dev unaffected.

**Effort:** ~10 min.

---

## 5. Stripe checkout — fail fast with a meaningful code (F-007) — Medium UX / Low backend
**File:** `backend/RentalManagementApi/Controllers/PaymentsController.cs` (or its service).

Returns `500 INTERNAL_ERROR` when the Stripe key is missing. Generic message is good (no leak), but the user has no clue what to do.

**Action:**
- Backend: at startup, validate `Stripe:SecretKey` is set; if not, log a warning and have the controller short-circuit to `503 STRIPE_NOT_CONFIGURED`.
- Frontend: once #1 is done, this falls out for free — the tenant sees "Online payment unavailable — use Manual Pay instead."

**Effort:** ~30 min.

---

## 6. Make CSP know about the production API origin (F-001 prod path) — Medium prod
**File:** `frontend/rental-management-ui/src/index.html`.

Dev backend (`http://localhost:8080`) was added in this sweep. Prod still won't work if the API is hosted on a different origin from the SPA (e.g. Vercel + Render). The current `connect-src` will block every API call.

**Action — pick one:**
- **A. Build-time substitution (simpler):** template `index.html` so `connect-src` is populated from `environment.apiUrl` at build time.
- **B. Move CSP to a server-rendered header (cleaner):** if the SPA is served from the .NET app (or a small edge function), set the CSP via an HTTP header that includes the current environment's API origin.

`environment.prod.ts` already reads `apiUrl` from a Vercel env var — (A) just plugs into the same value.

**Effort:** ~1 hour (build pipeline work).

---

## 7. Eagerly materialize the local User row on register (F-002) — Info / UX polish
**Symptom:** Immediately after registration, the SPA fires `/api/users/me` twice and gets 404 each time, until the User row syncs (currently triggered lazily on the first business-data call).

**Action — pick one:**
- **A.** In `AuthService.RegisterAsync`, return the new `User` and have the frontend cache it locally so the first `/api/users/me` resolves without a round-trip.
- **B.** Frontend treats 404 from `/api/users/me` as "user record being created" and retries once with backoff.

(A) is cleaner; (B) is defensive even if (A) lands.

**Effort:** ~30 min.

---

## Out-of-scope-but-worth-noting

These aren't findings — they came up while reading the code during the sweep:

- **Onboarding wizard overlay can intercept clicks** — caused at least one Playwright click failure during this run when the "Sign out" button was overlapped by the wizard. Visually it's fine; only an issue for automation. Consider auto-dismissing the wizard after first interaction, or making "Skip for now" sticky across reloads.
- **`/api/auth/invite/{token}` is anonymous and returns the invitee email.** Useful for the accept-invite UX, but does allow token-guessing attackers to map valid tokens to emails. Tokens are 22-char URL-safe base64 of a Guid — guessing is computationally hard, but a defense-in-depth move would be to require a CAPTCHA or to gate the email return behind first attempting a guess+miss penalty.
- **Stale logs and screenshots** at the repo root (`backend_err.txt`, `e2e-tc*.png`, `wave1-*.png`, `.playwright-mcp/`, `playwright-report/`, `test-results/`) — add to `.gitignore` and `git rm --cached` when convenient.

---

## Summary table

| # | Finding | Severity | Effort | File(s) |
|---|---|---|---|---|
| 1 | Surface backend errors in forms (F-003, F-007) | Med UX | ~30m × 2 | lease-form, payments-list |
| 2 | Cap pageSize | Low | ~20m | query DTOs |
| 3 | Custom InvalidModelStateResponseFactory | Low | ~10m | Program.cs |
| 4 | HSTS in production | Info (prod) | ~10m | SecurityHeadersMiddleware |
| 5 | Stripe fail-fast with meaningful code | Med UX | ~30m | PaymentsController |
| 6 | CSP knows production API origin | Med (prod) | ~1h | index.html / build pipeline |
| 7 | Eager User row materialization | Info | ~30m | AuthService / users.me |

Total estimated effort: ~4 hours of focused work to close every remaining issue from this sweep.
