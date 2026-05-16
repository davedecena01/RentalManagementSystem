# Rental Property Manager — E2E + Security Sweep Report

**Run:** 2026-05-17
**Branch:** `test/e2e-security-sweep`
**Method:** Live Playwright MCP (real browser) + direct API probing
**Accounts:** Fresh registration each run (landlord + tenant)

---

## Headline

| | |
|---|---|
| **Functional E2E** | 100% of in-scope flows pass: registration, login, role-gated nav, properties CRUD, leases (with provisions + PDF), maintenance lifecycle, dashboard metrics, tenant invite/accept |
| **Security** | 9 findings — **1 Critical** (dev-blocking, fixed mid-run), **2 High**, **2 Medium**, **3 Low**, **1 Info**. Auth fundamentals (JWT signing, IDOR, mass-assignment, SQLi, XSS, traversal, headers, rate limiting) all solid; gaps are concentrated in the anonymous registration / invite-acceptance flow |
| **Stripe** | Button wired; backend returns 500 (likely missing key in dev). Frontend silently swallows the error |

---

## Functional Test Matrix

### Authentication & Registration (Wave 1)
| ID | Test | Result |
|---|---|---|
| TC-REG-N01 | Empty register form submit | ✅ 4 inline validation errors |
| TC-REG-P01 | Valid registration | ✅ 200, auto-login, role=Landlord, onboarding wizard appears |
| TC-LOGIN-P01 | Valid login | ✅ 200, redirect to /dashboard |
| TC-INVITE-P01 | Landlord creates tenant invite | ✅ 200, token issued |
| TC-INVITE-P02 | Tenant accepts via /auth/accept-invite?token=… | ✅ 200, role=Tenant |
| TC-AUTHZ-NAV | Tenant nav hides landlord-only menu items | ✅ Properties / Leases / Clause Library hidden |

### Properties CRUD (Wave 2a)
| ID | Test | Result |
|---|---|---|
| TC-PROP-N01 | Empty Add Property submit | ✅ Required-field errors |
| TC-PROP-XSS | `<script>…</script>` in name + address | ✅ Stored as-is, rendered escaped, no execution |
| TC-PROP-P01 | Valid create | ✅ 201 |
| TC-PROP-P02 | Edit and save | ✅ Update reflected |
| TC-PROP-P03 | Delete | ✅ 204, removed from list |
| TC-PROP-DASH | Dashboard count reflects state | ✅ Properties=2, Vacant=2 |

### Leases + Clauses + Provisions + PDF (Wave 2b)
| ID | Test | Result |
|---|---|---|
| TC-LEASE-N01 | Empty lease form submit | ✅ 5 inline errors |
| TC-LEASE-N02 | End date < start date | ✅ Backend 400 `INVALID_DATE_RANGE` *(UI silent — see F-003)* |
| TC-LEASE-N03 | Unknown tenant email | ✅ Backend 404 `TENANT_NOT_FOUND` *(UI silent — F-003)* |
| TC-LEASE-P01 | Valid create | ✅ 201, full payload returned |
| TC-PROV-N01 | Empty provision-template submit | ✅ Title/Body required, max-length enforced |
| TC-PROV-XSS | XSS payload in template title + body | ✅ Stored raw, rendered escaped |
| TC-LEASE-PROV | PUT lease provisions | ✅ 200 |
| TC-LEASE-PDF | GET lease PDF | ✅ 200, `application/pdf`, magic bytes `%PDF-1.4`, 64KB |

### Maintenance + Dashboard (Wave 2c)
| ID | Test | Result |
|---|---|---|
| TC-MNT-N01 | Empty submit (tenant) | ✅ Title/Description required |
| TC-MNT-P01 | Tenant submits High-priority request | ✅ Listed status Open |
| TC-MNT-RESOLVE | Landlord resolves with notes | ✅ Status Open → Resolved |
| TC-DASH-LIVE | All metrics post-lease | ✅ Properties=2, Occupied=1, Vacant=1, MonthlyRent=₱15k, UnpaidRent=₱180k (12 × 15k auto-generated), OpenMaint=1→0 after resolve |

### Stripe / Payments (Wave 2d)
| ID | Test | Result |
|---|---|---|
| TC-PAY-LIST | Tenant sees own payment rows only (no landlord/tenant column) | ✅ |
| TC-PAY-STRIPE | "Pay Online" → `POST /api/leases/{id}/payments/stripe-checkout` | 🟠 500 INTERNAL_ERROR, UI silent — **F-007** |
| TC-PAY-MANUAL | "Manual Pay" modal renders (amount, notes, proof upload) | ✅ |

---

## Security / Penetration Test Matrix (Wave 3)

### Findings (full detail in [SECURITY_FINDINGS.md](SECURITY_FINDINGS.md))

| ID | Severity | Title | Status |
|---|---|---|---|
| F-001 | Critical-Dev / Medium-Prod | CSP `connect-src` excluded backend origin | 🔧 Fixed this run |
| F-002 | Info | `/api/users/me` 404 on first call after register | Open |
| F-003 | Medium-UX | Lease form silently swallows backend errors | Open |
| F-004 | Low | Default ProblemDetails leaks .NET namespaces | Open |
| F-005 | **High** | accept-invite trusts body `SupabaseUserId` (anonymous) — confirmed by repro | Open |
| F-006 | **High** | Same vulnerability class on `/api/auth/register` — confirmed by repro (5 fake users created) | Open |
| F-007 | Medium-UX / Low-Backend | Stripe checkout 500 with silent UI | Open |
| F-008 | Low | Pagination has no upper bound | Open |
| F-009 | Info (prod only) | HSTS not configured | Open |

### Negative results — these passed (no finding)
- **All 9 unauth API probes** → 401
- **All 10 vertical escalation probes (tenant → landlord)** → 403/401/405
- **All 5 JWT tampering probes** (alg:none, mutated payload, expired, garbage, malformed) → 401
- **Mass-assignment** — backend ignored over-posted `id`/`landlordId`/`role`/`email` fields, set `landlordId` from JWT
- **IDOR by fabricated GUID** — 404 `*_NOT_FOUND` (privacy-preserving)
- **SQL injection** — EF Core parameterized queries, payloads passed through harmlessly
- **Path traversal** — bucket regex `^[a-zA-Z0-9._-]+$` blocks `..`, `/`, null byte
- **CORS** — Specific origin (`http://localhost:4200`), not `*`
- **Security headers** — `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy`, `Permissions-Policy` all present
- **Rate limiting on /register and /accept-invite** — 5/min confirmed (200×5 then 429×5)
- **XSS** — Angular default escaping blocked all payloads (property name, lease clause, etc.)

---

## Environment
- Frontend: Angular dev server `http://localhost:4200`
- Backend: .NET 8 Web API `http://localhost:8080`
- Database: Supabase Postgres (cloud)
- Browser: Playwright Chromium via MCP

## Artifacts
- Persistent spec: [e2e/security-deep.spec.ts](e2e/security-deep.spec.ts) — re-runnable security regression suite
- Detail: [SECURITY_FINDINGS.md](SECURITY_FINDINGS.md)
- Screenshots: see `.playwright-mcp/` (from this run)

## Recommended Next Steps (prioritized)
1. **F-005 + F-006** — Patch `/api/auth/accept-invite` and `/api/auth/register` to require a Supabase JWT and derive `SupabaseUserId` from the JWT `sub` claim. Single PR.
2. **F-001 (prod path)** — Make `connect-src` build-time aware of the production API origin (or move CSP from meta tag to a server-rendered header).
3. **F-003 + F-007** — Wire a generic API-error banner in the lease form and payments page; the backend already returns clean `{error, code}` shapes.
4. **F-004** — Add custom `InvalidModelStateResponseFactory` so validation responses use the project's `ApiError` shape (no .NET namespace leak).
5. **F-008** — Cap `pageSize` server-side.
6. **F-009** — Add HSTS in prod.
7. **Cleanup** — Delete 6 junk User rows in DB (see SECURITY_FINDINGS.md Cleanup Checklist).
