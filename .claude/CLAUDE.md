# CLAUDE.md

## Project Overview

This repository is a portfolio-quality full-stack application called **Rental Property Manager**.

The application helps landlords and property managers manage rental properties, tenants, leases, rent payments, maintenance requests, reminders, and dashboard metrics.

Claude must treat this as a real engineering project, not a throwaway demo.

---

## Primary Goal

Build a clean, maintainable, demo-ready MVP for rental property management using a disciplined spec-driven workflow.

Prioritize:
- correctness
- security
- maintainability
- readable architecture
- practical MVP delivery
- free-tier-friendly deployment
- strong portfolio value

Do not prioritize:
- unnecessary complexity
- speculative abstractions
- enterprise patterns without value
- out-of-scope features
- verbose explanations
- rewriting large files without need

---

## Product Summary

The app helps landlords answer:

**"What is the current state of my rental properties, tenants, leases, payments, and maintenance?"**

Core workflows:
- landlord signs up and logs in
- landlord creates properties
- landlord adds tenants
- landlord assigns leases
- system generates and tracks rent payments
- tenants can pay through Stripe or upload manual payment proof
- tenants submit maintenance requests
- landlords resolve maintenance requests
- dashboard shows portfolio health and unpaid rent
- reminders are sent before and after rent due dates

---

## Primary Users

- **Landlord / Property Manager**: primary user who manages properties, tenants, leases, payments, and maintenance
- **Tenant**: views lease/payment information, pays rent, uploads payment proof, and submits maintenance requests
- **Staff / Administrator**: optional support role with limited management access

---

## Tech Stack

Use this stack unless explicitly instructed otherwise.

### Frontend
- Angular
- TypeScript
- SCSS or CSS
- Responsive SaaS-style dashboard UI

Do not switch to React or Next.js unless the user explicitly requests it.

### Backend
- C#
- .NET 8 Web API
- Entity Framework Core
- PostgreSQL

### Platform and Services
- Supabase PostgreSQL for database
- Supabase Auth for authentication
- Supabase Storage for tenant IDs, payment proofs, and maintenance images
- Stripe Checkout for online payments
- SendGrid for email reminders
- GitHub Actions for CI
- Vercel or another free-tier-friendly host for frontend
- Railway, Render, Fly.io, or another free-tier-friendly host for backend

### Development Assumptions
- Free-tier-friendly architecture is required.
- Secrets must be configured through environment variables.
- The app should be easy to demo locally and deploy publicly.

---

## Source of Truth

The `/docs` folder and `project-spec.md` are the source of truth.

Before implementing or changing any feature, Claude must:
1. check the relevant spec
2. identify the exact scope
3. follow the documented API/data/UI behavior
4. make only necessary assumptions
5. update docs only when the implemented behavior materially changes

Claude must not silently invent undocumented business rules.

---

## MVP Scope

### In Scope
- authentication and role-based access
- landlord registration and login
- tenant registration or tenant invite/link flow
- property CRUD
- property inventory items
- deposit and advance payment fields
- tenant management
- lease assignment
- rent scheduling
- payment records
- Stripe Checkout payment flow
- Stripe webhook handling
- manual payment proof upload
- partial/full/unpaid payment status
- maintenance request submission
- maintenance request resolution
- dashboard cards and charts
- configurable rent reminders
- email notifications through SendGrid
- user profile/settings
- basic logs
- health check endpoint
- CI build/test workflow
- responsive web UI

### Out of Scope
- native mobile app
- in-app chat
- multi-bank integrations
- advanced accounting
- complex AI/chatbot features
- multiple tenants per lease unless spec changes
- bulk import/export
- e-signature
- advanced leasing workflows
- SMS notifications
- microservices
- event-driven architecture
- heavy analytics stack

Protect the MVP from feature creep.

---

## Architecture Rules

Use a clean, practical layered architecture.

### Backend Structure

Recommended backend folders:
- `Controllers`
- `Application` or `Services`
- `Data`
- `Entities`
- `DTOs`
- `Repositories` or focused data access classes
- `Integrations`
- `Options`
- `Validation`
- `Middleware`
- `Jobs` or `ScheduledTasks`
- `Common`

### Backend Responsibilities
- Controllers stay thin.
- Business logic belongs in services.
- Data access belongs in repositories or focused EF Core query classes.
- API contracts use DTOs, not EF entities.
- External services are isolated behind interfaces/adapters.
- Auth and authorization checks must be explicit and consistent.
- Payment and webhook logic must be isolated from controllers.
- Reminder logic must be testable and runnable by a scheduled job endpoint or worker.
- File upload handling must validate type, size, and authorization.

### Frontend Structure

Recommended Angular folders:
- `core`
- `shared`
- `features/auth`
- `features/dashboard`
- `features/properties`
- `features/leases`
- `features/payments`
- `features/maintenance`
- `features/account`

Frontend responsibilities:
- API calls stay in Angular services.
- Components stay focused on UI and interaction.
- Forms use clear validation.
- Role-based UI visibility must match backend authorization.
- Loading, empty, success, and error states must be handled.
- Avoid complex state management unless justified.

---

## Domain Model

Core entities:
- User
- Property
- PropertyInventory
- Lease
- Payment
- MaintenanceRequest

Optional or supporting entities:
- ReminderSetting
- AppLog
- PaymentWebhookEvent
- FileUploadMetadata

Important relationships:
- Landlord owns many properties.
- Property has many inventory items.
- Property has leases.
- Lease belongs to one tenant.
- Lease has many payments.
- Property has many maintenance requests.
- Tenant can submit maintenance requests.

Use consistent key types across the app. Prefer `Guid` for new implementation unless the existing project already uses `int`.

---

## Role and Authorization Rules

Roles:
- `Landlord`
- `Tenant`
- `Staff` if implemented

Authorization rules:
- Landlords can access only properties, leases, payments, and maintenance records they own.
- Tenants can access only their own lease, payment history, and maintenance requests.
- Staff access must be scoped to a landlord account if implemented.
- Never rely only on frontend role checks.
- Every backend query must enforce tenant/owner isolation.

---

## API Design Rules

Use RESTful conventions.

Core endpoint groups:
- `/api/auth`
- `/api/users`
- `/api/properties`
- `/api/properties/{id}/inventory`
- `/api/leases`
- `/api/payments`
- `/api/payments/stripe-checkout`
- `/api/payments/stripe-webhook`
- `/api/maintenance`
- `/api/dashboard`
- `/api/reminders`
- `/healthz`

Rules:
- Use clear route names.
- Use appropriate HTTP status codes.
- Return consistent JSON error responses.
- Use pagination for lists when useful.
- Keep request/response DTOs explicit.
- Do not expose internal exception details.
- Stripe webhook must validate Stripe signature.
- Protected endpoints require valid JWT except public auth and webhook endpoints.

---

## Data and EF Core Rules

Claude must:
- use EF Core migrations
- keep entity configuration explicit where needed
- add indexes for common filters:
  - owner/user IDs
  - property ID
  - lease ID
  - payment due date
  - payment status
  - maintenance status
- validate decimal money fields
- avoid floating point for money
- use UTC for timestamps
- use date-only fields where appropriate for lease/payment due dates
- avoid cascading deletes that could accidentally remove important records unless clearly intended

Prefer service-level deletion rules over dangerous automatic data loss.

---

## Payment Rules

Payment statuses:
- `Unpaid`
- `Partial`
- `Paid`

Payment behavior:
- rent payments are tied to a lease and due date
- amount due must be stored
- amount paid must support partial payment
- manual payment can include proof upload
- Stripe payment should create a checkout session
- Stripe webhook confirms successful payment
- payment records must not be blindly trusted from the client
- webhook events should be idempotent where practical
- never store raw card data

---

## File Upload Rules

Supported uploads:
- tenant ID image/PDF
- manual payment proof image/PDF
- maintenance image

Rules:
- validate file type
- validate file size
- store files in Supabase Storage
- save file URL or storage path in DB
- prefer protected/signed access for sensitive files
- do not expose tenant ID documents to unauthorized users

---

## Reminder Rules

Reminder behavior:
- support before-due reminders
- support after-due reminders
- allow per-lease reminder day settings
- send email through SendGrid
- scheduled execution can be triggered by GitHub Actions, Railway Cron, or a secured endpoint

Rules:
- reminder logic must be testable
- avoid duplicate reminder sends where practical
- log reminder send attempts
- do not hardcode email API keys or sender secrets

---

## Dashboard Rules

Dashboard should include:
- total properties
- occupied count
- vacant count
- total monthly rent
- total unpaid rent
- open maintenance count
- total maintenance cost if repair cost is implemented
- monthly income chart
- paid vs unpaid chart

Dashboard data must be computed from authoritative backend data, not guessed on the frontend.

---

## Security Rules

Claude must:
- never hardcode secrets
- use environment variables for keys and connection strings
- validate all write operations
- enforce backend authorization
- avoid leaking internal exception details
- validate file uploads
- use HTTPS assumptions for deployed environments
- keep Stripe card handling fully inside Stripe Checkout
- avoid storing unnecessary PII
- keep tenant ID files access-controlled
- use JWT bearer authentication for protected API endpoints

---

## Error Handling Rules

Use consistent API error responses.

Distinguish:
- validation errors
- authentication errors
- authorization errors
- not found errors
- external provider errors
- unexpected server errors

Preferred response shape:
```json
{
  "error": "Human-readable message",
  "code": "ERROR_CODE"
}
```

Do not expose stack traces to API consumers.

---

## Testing Expectations

Prioritize meaningful tests.

Backend tests:
- auth/authorization checks
- property CRUD validation
- lease date validation
- payment status calculation
- manual payment behavior
- Stripe webhook idempotency
- dashboard aggregation
- reminder candidate selection
- maintenance status updates

Frontend tests:
- login/register validation
- property form validation
- lease form validation
- payment status display
- role-based UI visibility
- dashboard rendering with mocked API data

E2E tests:
- landlord signs up
- landlord creates property
- landlord assigns tenant/lease
- tenant submits maintenance request
- landlord resolves request
- manual payment proof is uploaded
- dashboard metrics update

Avoid excessive low-value test boilerplate.

---

## Coding Principles

Write code that is:
- simple
- readable
- maintainable
- testable
- secure
- portfolio-worthy

Prefer:
- explicit names
- small focused methods
- practical abstractions
- clear DTOs
- predictable folder structure
- service-level business rules

Avoid:
- clever code
- deep abstraction stacks
- generic enterprise boilerplate
- premature optimization
- unrelated refactors
- unnecessary dependencies
- large rewrites

---

## Dependency Rules

Claude must:
- minimize dependencies
- prefer built-in Angular and .NET features first
- add libraries only when they solve a real problem
- explain new dependencies briefly before adding them
- avoid heavy UI frameworks unless already chosen
- avoid paid-service assumptions unless explicitly approved

---

## UI and UX Rules

Use a clean rental SaaS dashboard style.

UI should include:
- left or top navigation
- role-aware menu items
- dashboard metric cards
- clear tables
- simple forms
- status badges
- clear empty states
- loading indicators
- validation messages
- confirmation dialogs for destructive actions

Prioritize usability over decoration.

The app must be responsive web, but desktop-first is acceptable for MVP.

---

## Token Conservation Rules

Claude must conserve tokens.

Response style:
- be concise by default
- summarize first
- use bullets when clearer
- avoid repeating specs
- avoid filler text
- do not explain obvious code line by line unless asked
- provide only relevant code
- avoid generating large files unless required

Implementation style:
- change only what is needed
- avoid touching unrelated files
- avoid duplicate helpers
- avoid placeholder files without purpose
- preserve existing structure where reasonable

Clarification:
- ask only when the answer materially affects architecture, correctness, security, or scope
- if a reasonable assumption is safe, state it briefly and proceed

---

## Mandatory Branching and Commit Workflow

Claude must never commit or push directly to `main`.

Before any major code, documentation, or structural change, Claude must:
1. check the current git branch
2. determine whether the branch matches the current task
3. create a new branch if on `main`
4. create a new branch if the current branch name does not match the task
5. use one of these prefixes:
   - `feature/description`
   - `fix/description`
   - `refactor/description`
   - `docs/description`
6. state the branch name before implementation begins
7. keep changes scoped to the branch purpose

Branch reuse is allowed only when the new task clearly belongs to the same unit of work.

Examples:
- `feature/property-crud`
- `feature/lease-assignment`
- `feature/stripe-payments`
- `feature/manual-payment-proof`
- `feature/maintenance-requests`
- `feature/dashboard-metrics`
- `fix/payment-status-calculation`
- `docs/setup-guide`

Treat working on `main` or reusing an unrelated branch as a workflow violation.

### Commit Style

Use semantic commit messages.

Examples:
- `feat(auth): add Supabase JWT validation`
- `feat(properties): implement property CRUD`
- `feat(leases): add tenant lease assignment`
- `feat(payments): add Stripe checkout flow`
- `feat(payments): support manual proof upload`
- `feat(maintenance): add request resolution flow`
- `feat(dashboard): add portfolio metrics`
- `fix(payments): correct partial payment status`
- `docs: update setup instructions`

---

## Workflow for Claude

### Before implementation
1. review relevant specs
2. identify exact scope
3. confirm branch workflow
4. produce a short plan when useful
5. avoid unnecessary questions

### During implementation
1. implement one scoped change at a time
2. keep code localized
3. follow existing patterns
4. preserve API contracts unless intentionally changing them
5. keep security and authorization checks explicit

### After implementation
1. review changed files
2. verify against acceptance criteria
3. run or suggest relevant tests
4. suggest doc updates only if needed
5. provide a concise summary of changes

---

## Claude Review Behavior

When reviewing code, evaluate:
- correctness
- security
- authorization/data isolation
- maintainability
- architecture consistency
- validation gaps
- payment/webhook safety
- file upload safety
- error handling
- test coverage
- unnecessary complexity
- portfolio quality

Give practical feedback, not academic critique.

---

## Documentation Expectations

Keep documentation concise and useful.

Maintain or suggest updates for:
- `README.md`
- `project-spec.md`
- `docs/setup.md`
- `docs/architecture.md`
- `docs/api-contracts.md`
- `docs/deployment.md`
- `docs/changelog.md`

Do not rewrite documentation unnecessarily.

---

## Important Guardrails

Claude must not:
- add features outside MVP scope without approval
- add paid-service dependencies without warning
- hardcode secrets
- skip backend authorization checks
- expose tenant/private files publicly without intent
- trust client-side role checks
- rewrite large areas without justification
- silently change API contracts
- add AI/chatbot features to this MVP
- introduce microservices or heavy infrastructure
- create bloated code or docs

Claude should optimize for:
- clean MVP delivery
- reliable demo flow
- secure landlord/tenant data isolation
- maintainable code
- simple deployment
- strong portfolio presentation
