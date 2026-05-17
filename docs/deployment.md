# Deployment Guide

Frontend → **Vercel**. Backend → **Railway**. Database, Auth, Storage → **Supabase**.

Deploy backend first (so the frontend has a real `apiUrl`), then frontend, then wire CORS + webhooks.

---

## 1. Prerequisites

- A Supabase project with:
  - URL, anon key, service-role key, JWT secret (Settings → API)
  - Private storage buckets: `tenant-ids`, `payment-proofs`, `maintenance-images`, `lease-documents`
- A Stripe account (test mode is fine) for `SecretKey`, `PublishableKey`, and later the webhook secret
- A SendGrid account with a verified sender for `ApiKey`, `FromEmail`
- The GitHub repo connected to Vercel + Railway

---

## 2. Backend → Railway

### 2.1 Create the service

1. **railway.app** → New Project → Deploy from GitHub repo.
2. Set the **root directory** to `backend/RentalManagementApi` (Settings → Source).
3. Railway will detect the `Dockerfile` and build automatically.

### 2.2 Environment variables

In Railway → Variables, add:

```
ASPNETCORE_ENVIRONMENT       = Production
PORT                         = 8080         # Railway sets this automatically; included for clarity
FRONTEND_URL                 = https://<vercel-domain>     # fill after step 3

ConnectionStrings__DefaultConnection = Host=aws-x-x.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.<project-ref>;Password=<db-password>;SSL Mode=Require;Trust Server Certificate=true;Pooling=true

Supabase__Url                = https://<project-ref>.supabase.co
Supabase__AnonKey            = <anon-key>
Supabase__ServiceRoleKey     = <service-role-key>
Supabase__JwtSecret          = <jwt-secret>

Stripe__SecretKey            = sk_test_...
Stripe__PublishableKey       = pk_test_...
Stripe__WebhookSecret        = whsec_...                  # fill after step 4

SendGrid__ApiKey             = SG....
SendGrid__FromEmail          = noreply@yourdomain.com

App__FrontendUrl             = https://<vercel-domain>    # fill after step 3
App__InternalApiKey          = <random-32-char-string>
App__EnableSeedEndpoint      = false                      # set to true only for demos
```

> Use the Supabase **pooler** URL (port `6543`) — Railway can't reach the direct connection (port `5432`) reliably.

### 2.3 Generate a public domain

Railway → Settings → Networking → **Generate Domain**. Copy the URL — this is your `apiUrl`.

### 2.4 Run database migrations (Option B — manual)

After the first deploy, apply the schema with the Railway CLI:

```bash
# one-time setup
npm i -g @railway/cli
railway login
railway link            # pick the project

# inside backend/RentalManagementApi
railway run dotnet ef database update
```

Re-run that command after every deploy that includes a new migration.

---

## 3. Frontend → Vercel

### 3.1 Import the project

1. **vercel.com** → New Project → Import the GitHub repo.
2. Set the **Root Directory** to `frontend/rental-management-ui`.
3. Framework Preset: **Other** (the `vercel.json` already wires the Angular build).
4. Build Command, Output Directory, Install Command: leave as-is — they come from `vercel.json`.

### 3.2 Environment variables

Vercel → Settings → Environment Variables (Production scope):

```
NG_API_URL                 = https://<railway-domain>/api
NG_SUPABASE_URL            = https://<project-ref>.supabase.co
NG_SUPABASE_ANON_KEY       = <anon-key>
NG_STRIPE_PUBLISHABLE_KEY  = pk_test_...
```

These flow into `scripts/set-env.js` at build time, which writes `src/environments/environment.ts` before `ng build` runs.

### 3.3 Deploy

Click **Deploy**. Copy the resulting `https://<vercel-domain>` URL.

---

## 4. Wire up the two sides

1. **Railway env vars** → set `FRONTEND_URL` and `App__FrontendUrl` to the Vercel URL. Redeploy.
2. **Stripe Dashboard** → Developers → Webhooks → Add endpoint:
   - URL: `https://<railway-domain>/api/payments/stripe-webhook`
   - Events: `checkout.session.completed`, `payment_intent.succeeded`, `payment_intent.payment_failed`
   - Copy the signing secret → set `Stripe__WebhookSecret` on Railway. Redeploy.
3. **Supabase Auth** → URL Configuration:
   - Site URL: `https://<vercel-domain>`
   - Redirect URLs: `https://<vercel-domain>/**`

---

## 5. Smoke test the live deployment

1. Open `https://<vercel-domain>` → register a new landlord.
2. Open the Network tab — requests should hit `https://<railway-domain>/api/...` and return 200, not CORS-block.
3. Add a property, create a lease, mark a payment paid.
4. Hit `https://<railway-domain>/healthz` directly — should return 200.

---

## 6. Common pitfalls

| Symptom | Cause | Fix |
|---|---|---|
| Frontend 401s every request | Backend env missing `Supabase__JwtSecret` or wrong project | Re-copy from Supabase → Settings → API |
| CORS error in browser | `FRONTEND_URL` not set or has trailing slash | Match the Vercel URL exactly, no slash |
| Login works but dashboard is empty / tenant nav shows | Rate limit on `/api/auth/register` hit during signup | Wait 1 min and try again; for demos, increase the limit in `Program.cs` |
| Stripe webhook ignored | Wrong webhook secret | Re-copy from Stripe Dashboard → webhook details |
| 500 on first request after deploy | Migrations not applied | Run `railway run dotnet ef database update` |
| Vercel build fails with "missing NG_API_URL" | Env vars not set in Vercel Production scope | Set them, re-trigger deploy |
