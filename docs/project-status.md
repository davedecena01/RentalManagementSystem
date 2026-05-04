# Project Status — Rental Property Manager

**Last Updated:** 2026-05-05
**Current Wave:** Wave 1 — Foundation & Auth (Not Started)

---

## Overall Progress

| Wave | Name | Status | Notes |
|------|------|--------|-------|
| 1 | Foundation & Auth | Not Started | Backend skeleton, Supabase auth, Angular shell, tenant invite, live deploy |
| 2 | Properties, Leases, Payments | Not Started | Core CRUD, Stripe, manual payment proof |
| 3 | Maintenance, Dashboard, Reminders | Not Started | Analytics, charts, email reminders cron |
| 4 | PDF + Audit Log + Onboarding + Polish | Not Started | Differentiator features, seed data, demo page |

---

## Active Work

_Nothing in progress yet. Documentation refinement complete — ready to begin Wave 1._

---

## Completed Milestones

- [x] **2026-05-05** — Project documentation scaffolded (initial)
- [x] **2026-05-05** — Spec refined: locked decisions, restructured into 4 waves, added differentiator and backlog sections, fixed all inconsistencies

---

## Blockers

_None at this time._

---

## Decisions Made

| Date | Decision | Choice | Rationale |
|------|----------|--------|-----------|
| 2026-05-05 | Frontend framework | Angular | Matches CLAUDE.md; differentiates from typical React portfolio projects |
| 2026-05-05 | Currency | PHP (₱), hard-coded | Matches PH example data; simplifies dashboard aggregation; no multi-currency in MVP |
| 2026-05-05 | Tenant onboarding | Landlord invite only (email link) | Cleaner UX matching premium SaaS; no unlinked-tenant edge case |
| 2026-05-05 | Portfolio audience | Freelance/agency clients | Emphasis on polish, completeness, and a live working demo |
| 2026-05-05 | Scope | MVP + 3 standout features (6–8 weeks) | Best balance of impact vs. effort |
| 2026-05-05 | Standout feature group | Polish & Trust: PDF generation, Audit log, Onboarding wizard | Highest visible differentiation vs. free-tier competitors |
| 2026-05-05 | Demo strategy | Live deployment with seed data and public demo logins | Required for freelance/agency client demos |
| 2026-05-05 | Primary key type | Guid across all entities | CLAUDE.md preference; avoids enumeration attacks |
| 2026-05-05 | PDF library | QuestPDF (MIT, .NET 8) | Free, well-maintained, clean API; no paid service needed |
| 2026-05-05 | Backend hosting | Railway (primary) | Free-tier-friendly, Docker/Nixpacks, easy env vars |
| 2026-05-05 | Staff/Accountant role | Deferred to backlog | Adds auth complexity with low MVP ROI |

---

## Open Questions

| # | Question | Impact |
|---|----------|--------|
| 1 | Hangfire or Railway Cron + GitHub Actions for reminder job? | Affects reminder reliability and deployment complexity |
| 2 | `TenantProfile` extension table, or store `tenantIdUrl` on `Users`? | Minor schema decision before Wave 1 EF migration |
| 3 | Soft delete (IsDeleted flag) or hard delete for properties/leases? | Affects audit log accuracy and cascade rules |

---

## Upcoming (Wave 1 Start Checklist)

1. Resolve Open Question #2 (tenantIdUrl on Users vs. separate table)
2. Create Supabase project and note all credentials
3. Create Stripe account (test mode)
4. Create SendGrid account and verify sender email
5. Create Railway account
6. Connect GitHub repo to Vercel
7. Begin Wave 1 backend: create .NET 8 project, add EF Core, configure Supabase JWT auth
8. Begin Wave 1 frontend: scaffold Angular project with layout shell

---

## Notes

Update this file at the start and end of each session. Mark waves as "In Progress" when the first task begins and "Complete" when the wave demo passes. Capture new decisions and blockers promptly.
