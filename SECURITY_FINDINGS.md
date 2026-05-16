# Security & E2E Findings — test/e2e-security-sweep

**Run start:** 2026-05-17
**Branch:** test/e2e-security-sweep
**Scope:** Live Playwright E2E (positive / negative / invalid) + security penetration. Payments limited to checkout-button smoke.

---

## Severity Legend
- **Critical** — exploitable now, data loss / unauthorized data access / app unusable
- **High** — exploitable with effort, or breaks a core flow
- **Medium** — defense-in-depth gap, low-likelihood exploitation
- **Low** — minor hardening
- **Info** — observation, no action required

---

## Findings

### F-001 — CSP `connect-src` excludes backend origin (Critical-Dev / Medium-Prod)
- **File:** [frontend/rental-management-ui/src/index.html:10](frontend/rental-management-ui/src/index.html#L10)
- **Introduced by:** commit `a2741f6` ("security: add HTTP security headers middleware and CSP meta tag")
- **Symptom:** Every XHR from the SPA to `http://localhost:8080/api/*` is blocked by the browser CSP. Dashboard renders empty; `/api/users/me`, `/api/dashboard`, etc. all fail with "violates the following Content Security Policy directive". App is non-functional in dev mode.
- **Root cause:** `connect-src` lists `'self'`, Supabase, and Stripe, but not the cross-origin dev backend.
- **Fix applied (this run):** Added `http://localhost:8080` to `connect-src` in index.html.
- **Production impact:** Same defect will reappear in any environment where the API is hosted on a different origin than the SPA (e.g. Vercel frontend + Render/Railway backend). The deployed API origin MUST be added to the prod CSP. Currently the prod build will silently break the moment the SPA is hosted separately from the API.
- **Recommendation:** Make the API origin part of the CSP a build-time variable derived from `environment.apiUrl`, or move CSP from a static meta tag to a server-rendered header that knows the deployed origin.

---

### F-002 — `/api/users/me` returns 404 immediately after registration (Info)
- **Symptom:** Twice on first dashboard load after register, `/api/users/me` 404s. Subsequent calls (properties, leases) work normally — the User row materializes when needed.
- **Impact:** None functionally, but produces noisy 404s in browser console and a momentary undefined-user flash in the UI.
- **Recommendation:** Either eagerly create the local User row in the register flow, or have the frontend treat 404 here as "user pending" and retry once.

### F-003 — Lease form silently swallows backend errors (Medium-UX)
- **File:** lease create form ([http://localhost:4200/leases/new]) in `frontend/rental-management-ui/src/app/features/leases/lease-form/*` (component for `LeaseFormComponent`)
- **Symptom:** Backend correctly rejects with `{error, code}` for `INVALID_DATE_RANGE`, `TENANT_NOT_FOUND`, etc. Frontend shows no message; form just sits.
- **Repro:** Sign in as landlord → New Lease → Property: any vacant → Tenant Email: `bogus@nowhere.test` → valid dates → Create Lease. Backend returns 404 TENANT_NOT_FOUND; UI is silent.
- **Recommendation:** Wire the existing `ApiError` shape into a form-level error banner; existing pattern in [register.component.ts] already does this.

### F-004 — Default ProblemDetails leaks internal namespaces (Low / Info disclosure)
- **Symptom:** ASP.NET's default model-validation 400 response includes fully-qualified .NET type names:
  ```
  "The JSON value could not be converted to System.Collections.Generic.List`1[RentalManagementApi.DTOs.Provisions.LeaseProvisionPayload]"
  ```
- **Impact:** Reveals backend tech stack (.NET) and internal DTO namespace structure (`RentalManagementApi.DTOs.Provisions.*`). Aids reconnaissance.
- **Repro:** `PUT /api/leases/{id}/provisions` with body `{provisions:[...]}` instead of bare array.
- **Recommendation:** Add a global ASP.NET exception/validation handler (`InvalidModelStateResponseFactory`) that returns the project's standard `ApiError` shape (`{error, code}`) without framework type names.

### F-005 — Tenant accept-invite trusts SupabaseUserId from request body without JWT validation — **CONFIRMED HIGH**
- **File:** [backend/RentalManagementApi/Controllers/AuthController.cs:57-75](backend/RentalManagementApi/Controllers/AuthController.cs#L57-L75)
- **Risk:** `POST /api/auth/accept-invite` is `[AllowAnonymous]` and takes `SupabaseUserId` from the request body (`AcceptInviteAsync` parses `Guid.Parse(request.SupabaseUserId)` directly into the User row). Unlike `/api/auth/register`, there is no check that a valid Supabase JWT was presented and that its `sub` claim matches.
- **Theoretical attack:** Anyone who learns an unused invite `Token` (e.g. from a leaked email, the unauthenticated `GET /api/auth/invite/{token}` endpoint, or log files) can create a Tenant `User` row bound to an attacker-controlled Supabase ID — granting the attacker the tenant account on next login.
- **Compounding:** `GET /api/auth/invite/{token}` is also `[AllowAnonymous]` and returns the invite's email. A token-guessing attacker could enumerate valid invites.
- **To verify in Wave 3:** Issue an invite, then attempt to accept it with a fabricated Supabase ID (no JWT). If the user row is created, this becomes a High-severity account-takeover via invite-token exposure.
- **Recommendation:** Require an authenticated Supabase JWT on accept-invite; derive `SupabaseUserId` from the JWT `sub` claim rather than the request body, the same way register does (auth.cs:14-31 already implements this guard for `/register`).
- **Verification (this run):**
  - Reproducer: as anonymous, with fresh email + `crypto.randomUUID()` UUID, `POST /api/auth/accept-invite` returns `200 {"id":"492d32c2-da05-43ce-ac51-f576bc9686dc","role":"Tenant"}` — proves the attack succeeds.
  - Also confirmed: `GET /api/auth/invite/{token}` anonymous returns `{"email":"…"}` — token enumeration leaks emails.
  - Cleanup needed: a junk `User` + `TenantProfile` row exists in the DB from the reproducer (id `492d32c2-da05-43ce-ac51-f576bc9686dc`); recommend deleting after the fix.

### F-006 — Same vulnerability class as F-005 on `/api/auth/register` — **HIGH**
- **File:** [backend/RentalManagementApi/Controllers/AuthController.cs:14-31](backend/RentalManagementApi/Controllers/AuthController.cs#L14-L31)
- **Issue:** The token-vs-body validation only runs `if (jwtSub is not null)`. When the request includes no JWT at all, the check is silently skipped, even though the endpoint is `[AllowAnonymous]` and accepts `SupabaseUserId` directly from the body.
- **Verified (this run):** Sent 5 anonymous `POST /api/auth/register` requests with `crypto.randomUUID()` for `supabaseUserId` and junk emails. All 5 returned 200 with a real User row created. (6th onward returned 429 thanks to the existing rate limit — which is good defense-in-depth but does not close the hole.)
- **Cleanup needed:** 5 junk landlord User rows persisted in DB with emails matching `flood-{0..4}-{timestamp}@test.test`.
- **Impact:**
  - DB pollution at up to 5/min/IP
  - Email squatting: backend has unique index on `Users.Email`; attacker can pre-claim emails of high-value targets, breaking their future Supabase-driven onboarding (server returns generic error, real user can't onboard)
- **Recommendation:** Make the auth guard symmetrical. Either (a) require a Supabase JWT on `/register` and derive `sub` from it, or (b) explicitly reject the request when `jwtSub is null` even though `[AllowAnonymous]` is set. Same fix should cover `/accept-invite` (F-005). Re-index findings: keep F-005 as the invite-flow issue; this F-006 is the corresponding register-flow issue.

### F-007 — Stripe checkout returns 500 INTERNAL_ERROR with silent UI (Medium-UX / Low-Backend)
- **Symptom:** `POST /api/leases/{leaseId}/payments/stripe-checkout` returns 500. Frontend swallows it (same F-003 pattern).
- **Likely root cause:** Stripe API key not configured in dev (`appsettings.Development.json` likely missing or env var unset).
- **Recommendations:**
  - Backend: validate Stripe configuration at startup; return `503 STRIPE_NOT_CONFIGURED` (or `501`) instead of 500 if the integration is unreachable.
  - Frontend: surface a banner to the tenant explaining online payment is unavailable.

### F-008 — Pagination has no upper bound (Low / DoS)
- **Symptom:** `GET /api/properties?pageSize=99999` returns the full list. No max-page-size cap.
- **Impact:** Low for the current MVP (small dataset), but on a populated DB a single query can consume a lot of memory and bandwidth.
- **Recommendation:** Clamp pageSize to a sane max (e.g., 100) in the repository or via a `[Range]` attribute on the query DTO.

### F-009 — HSTS not configured (Info; only relevant in prod)
- **Symptom:** `GET /healthz` and other API responses lack `Strict-Transport-Security`. Other security headers (X-Frame-Options DENY, X-Content-Type-Options nosniff, Referrer-Policy, Permissions-Policy) are present.
- **Impact:** Zero in dev (HTTP localhost); in production over HTTPS, MITM downgrade attacks become harder if HSTS is set.
- **Recommendation:** Add `Strict-Transport-Security: max-age=31536000; includeSubDomains` in the headers middleware, gated to production environment.

---
## Tests with Negative Results (no finding, all good)
- **CSRF** — All state-changing endpoints require Bearer JWT; no cookie auth → CSRF not applicable.
- **JWT tampering** — All 5 forged tokens (alg:none with mutated role; mutated payload + original sig; expired; garbage HS256; malformed) returned 401. Supabase ES256 signing properly verified.
- **Vertical escalation (tenant → landlord)** — All 10 probes returned 401/403/405 (POST property, DELETE landlord property, GET landlord property by id, POST lease, POST/GET provision-template, invite-tenant, resolve maintenance, PUT lease provisions).
- **Unauthenticated API access** — All 9 endpoints returned 401 without JWT.
- **Mass-assignment / over-posting** — `POST /api/properties` with extra `id`, `landlordId`, `role`, `email` in body returned 201 but the persisted row used the JWT's user as landlordId; extra fields ignored.
- **IDOR with fabricated GUID** — Returns 404 `PROPERTY_NOT_FOUND` / `LEASE_NOT_FOUND` (privacy-preserving, doesn't leak existence).
- **SQL injection** — EF Core parameterized queries; payloads `'; DROP TABLE Properties; --` and `Open' OR '1'='1` returned legitimate data without breakage.
- **Path traversal in file upload** — Blocked by regex `^[a-zA-Z0-9._-]+$` (no `/`, no `..`, no null byte).
- **Bucket whitelist** — Only `payment-proofs`, `tenant-ids`, `maintenance-images` allowed; signed URLs only for private buckets.
- **Rate limiting on /register and /accept-invite** — Confirmed 5/min cap (200×5 then 429×5).
- **XSS** — Angular default text interpolation escapes payloads in property name, address, clause title, clause body, and rendered tables. None executed.
- **Output escaping** — Backend stores raw HTML/JS strings; relies on frontend escaping. Defense-in-depth gap (Low) — would recommend HTML-sanitizing on input for human-facing string fields if rich text is ever added.
- **Role-based UI gating** — Tenant nav correctly hides Properties / Leases / Clause Library / Payments-as-landlord-view; tenant dashboard returns tenant-scoped data only.

---
## Severity Summary
| Sev | Count | IDs |
|-----|-------|-----|
| Critical | 1 (dev-blocking, fixed) | F-001 |
| High | 2 | F-005, F-006 |
| Medium | 2 | F-003, F-007 |
| Low | 3 | F-004, F-008, F-009 |
| Info | 1 | F-002 |

---
## Cleanup Checklist (post-run)
- [ ] Delete junk User rows in DB: `WHERE email LIKE 'flood-%-%@test.test'` (5 rows from F-006 repro) and `id = '492d32c2-da05-43ce-ac51-f576bc9686dc'` (F-005 repro).
- [ ] Test landlord and tenant accounts (`landlord-1778957813265@e2e.test` / `tenant-1778957813265@e2e.test`) can be left or deleted as desired.
