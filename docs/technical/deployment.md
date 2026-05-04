# Deployment Guide — Rental Property Manager

## Overview

| Component | Platform | Notes |
|-----------|---------|-------|
| Angular SPA | **Vercel** | Auto-deploy from `main` branch |
| .NET 8 API | **Railway** (or Render / Fly.io) | Dockerfile or Nixpacks |
| PostgreSQL | **Supabase** | Free tier |
| Auth + Storage | **Supabase** | Same project |
| Email | **SendGrid** | Free tier: 100 emails/day |
| Payments | **Stripe** | Test mode during development |
| Daily cron | **GitHub Actions** | Triggers reminder endpoint at 02:00 UTC |

All secrets in environment variables. Nothing hardcoded.

---

## Prerequisites

- Node.js 20+ and Angular CLI (`npm i -g @angular/cli`)
- .NET 8 SDK
- Supabase project created (free tier)
- Stripe account (test mode keys)
- SendGrid account + verified sender email
- Vercel account connected to GitHub repo
- Railway account (or Render / Fly.io)

---

## Environment Variables

### Backend (.NET API)

| Variable | Description | Where to Find |
|----------|-------------|---------------|
| `DATABASE_URL` | Supabase PostgreSQL URI | Supabase → Settings → Database → URI |
| `SUPABASE_URL` | Supabase project URL | Supabase → Settings → API |
| `SUPABASE_ANON_KEY` | Public anon key | Supabase → Settings → API |
| `SUPABASE_SERVICE_ROLE_KEY` | Service role key (server-only) | Supabase → Settings → API |
| `SUPABASE_JWT_SECRET` | JWT signing secret | Supabase → Settings → API → JWT Settings |
| `STRIPE_SECRET_KEY` | Stripe secret key | Stripe → Developers → API Keys |
| `STRIPE_WEBHOOK_SECRET` | Webhook signing secret | Stripe → Developers → Webhooks |
| `SENDGRID_API_KEY` | SendGrid API key | SendGrid → Settings → API Keys |
| `SENDGRID_FROM_EMAIL` | Verified sender email | Must be verified in SendGrid |
| `SENDGRID_FROM_NAME` | Sender display name | e.g. "Rental Property Manager" |
| `INTERNAL_API_KEY` | Secures reminder trigger endpoint | Generate with `openssl rand -hex 32` |
| `FRONTEND_URL` | Frontend URL (CORS + Stripe redirects) | Your Vercel deployment URL |
| `PDF_STORAGE_BUCKET` | Supabase Storage bucket for PDFs | `lease-documents` |
| `DEMO_MODE` | Enables seed data endpoint | `false` (set `true` only on staging) |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production` |
| `PORT` | API port (Railway injects this) | `8080` default |

### Frontend (Angular)

Set in `src/environments/environment.prod.ts` or as Vercel environment variables:

| Variable | Description |
|----------|-------------|
| `apiUrl` | Backend API base URL |
| `supabaseUrl` | Supabase project URL |
| `supabaseAnonKey` | Supabase anon/public key |
| `stripePublishableKey` | Stripe publishable key (`pk_test_...`) |

---

## Local Development Setup

### 1. Clone the repo
```bash
git clone https://github.com/<your-username>/RentalManagementSystem.git
cd RentalManagementSystem
```

### 2. Backend setup
```bash
cp .env.example .env
# Fill in your Supabase, Stripe, SendGrid values

cd backend/RentalManagementApi
dotnet restore
dotnet ef database update
dotnet run
# API runs at https://localhost:7000
```

### 3. Frontend setup
```bash
cd frontend/rental-management-ui
npm install
# Update src/environments/environment.ts with local API URL and Supabase keys
ng serve
# App at http://localhost:4200
```

### 4. Local Stripe webhook forwarding
```bash
stripe listen --forward-to https://localhost:7000/api/payments/stripe/webhook
# Copy the webhook signing secret shown → set STRIPE_WEBHOOK_SECRET in .env
```

---

## Supabase Setup

### 1. Create project
- [supabase.com](https://supabase.com) → New project
- Note: **Project URL**, **Anon Key**, **Service Role Key**, **JWT Secret**

### 2. Create Storage buckets
Create all four buckets as **private** (no public access):

| Bucket | Purpose |
|--------|---------|
| `tenant-ids` | Tenant ID uploads |
| `payment-proofs` | Manual payment proof files |
| `maintenance-images` | Maintenance request photos |
| `lease-documents` | Generated lease agreement and receipt PDFs |

### 3. Configure Auth
- Supabase → Authentication → Providers → Enable **Email**
- Set **Site URL** to your frontend URL (e.g. `https://your-app.vercel.app`)
- Add `http://localhost:4200` to **Additional Redirect URLs** for local dev

---

## Backend Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RentalManagementApi.dll"]
```

In `Program.cs` — ensure the port respects the Railway-injected `PORT`:
```csharp
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://+:{port}");
```

---

## Railway Deployment (Backend)

1. New project → **Deploy from GitHub repo** → select this repo
2. Set **Root Directory** to `backend/RentalManagementApi`
3. Railway → Variables → add all backend environment variables
4. Start command (runs migrations then starts API):
   ```bash
   dotnet ef database update && dotnet RentalManagementApi.dll
   ```
5. Note the public Railway URL → set as `ASPNETCORE_URLS` and add to Vercel's `apiUrl`

---

## Vercel Deployment (Frontend)

1. Import repo from GitHub
2. Set **Root Directory** to `frontend/rental-management-ui`

| Setting | Value |
|---------|-------|
| Build Command | `ng build --configuration production` |
| Output Directory | `dist/rental-management-ui/browser` |
| Install Command | `npm install` |

3. Vercel → Settings → Environment Variables → add frontend variables
4. Add `vercel.json` to Angular project root for SPA routing:
   ```json
   {
     "rewrites": [{ "source": "/(.*)", "destination": "/index.html" }]
   }
   ```

---

## Stripe Webhook (Production)

1. Stripe Dashboard → Developers → Webhooks → **Add endpoint**
2. URL: `https://<railway-api-host>/api/payments/stripe/webhook`
3. Events: `checkout.session.completed`
4. Copy **Signing Secret** → set `STRIPE_WEBHOOK_SECRET` in Railway

---

## GitHub Actions CI

`.github/workflows/ci.yml`:
```yaml
name: CI

on:
  push:
    branches: [main, "feature/**", "docs/**", "fix/**"]
  pull_request:
    branches: [main]

jobs:
  backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0'
      - run: dotnet restore
        working-directory: backend/RentalManagementApi
      - run: dotnet build --no-restore
        working-directory: backend/RentalManagementApi
      - run: dotnet test --no-build
        working-directory: backend/RentalManagementApi

  frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '20'
      - run: npm ci
        working-directory: frontend/rental-management-ui
      - run: npx ng build --configuration production
        working-directory: frontend/rental-management-ui
      - run: npx ng test --watch=false --browsers=ChromeHeadless
        working-directory: frontend/rental-management-ui
```

---

## Rent Reminder Cron (GitHub Actions)

`.github/workflows/reminders.yml`:
```yaml
name: Daily Rent Reminders

on:
  schedule:
    - cron: '0 2 * * *'   # 02:00 UTC daily

jobs:
  send-reminders:
    runs-on: ubuntu-latest
    steps:
      - name: Trigger reminder endpoint
        run: |
          curl -X POST ${{ secrets.API_URL }}/api/reminders/trigger \
            -H "X-Internal-Key: ${{ secrets.INTERNAL_API_KEY }}" \
            -H "Content-Type: application/json"
```

Add `API_URL` and `INTERNAL_API_KEY` to GitHub repo secrets.

---

## Seed Data (Demo Deployment)

When `DEMO_MODE=true` and the database is empty, the seeder runs automatically on startup (or via a secured endpoint `POST /api/seed` in demo environments only).

**Seed data created:**

| Type | Count | Details |
|------|-------|---------|
| Demo landlord | 1 | landlord@demo.com / password: `Demo1234!` |
| Demo tenants | 2 | tenant1@demo.com, tenant2@demo.com / `Demo1234!` |
| Properties | 3 | Mix of Bare/Semi/Fully furnished in Cebu City |
| Leases | 2 | 12-month active leases at ₱8,000 and ₱12,000/month |
| Payments | ~24 | Mix of Paid, Partial, Unpaid across both leases |
| Maintenance | 3 | Open, InProgress, and Resolved examples |
| Audit log | ~30 | Realistic history spanning past 3 months |
| Lease documents | 4 | Pre-generated lease PDFs and 2 receipts |

**Demo credentials page:** the Angular app has a public `/demo` route showing login credentials — no auth required to view it. This allows clients to click "Try as Landlord" or "Try as Tenant" directly.

---

## Monitoring

- **Railway** — built-in request logs, crash alerts, HTTP monitoring
- **Vercel** — function logs, build history, preview deploys on PRs
- **Supabase** — SQL editor, Auth logs, Storage usage dashboard
- **Stripe** — Developers → Events for webhook delivery status
- **SendGrid** — Activity Feed for email delivery status
- **`/healthz` endpoint** — can be pinged by an external uptime monitor (UptimeRobot free tier) to keep Supabase from pausing due to inactivity

---

## Rollback

1. Identify last good commit: `git log --oneline`
2. Railway → Deployments → select a previous deployment → Redeploy
3. Vercel → Deployments → select a previous deployment → Promote to Production
4. If DB migration rollback needed: generate a manual SQL script via `dotnet ef migrations script <previous> <current>` and run in reverse
