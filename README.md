# ONE. Competitions

ONE. Competitions is a multi-tenant SaaS platform for competition and giveaway management. It implements the standard-draw workflow from tenant/domain setup through public entries, review, immutable draws, winner management, CSV exports, and publicly verifiable PDF draw certificates.

## Architecture

- `apps/api`: ASP.NET Core Clean Architecture API with PostgreSQL, Identity, REST/OpenAPI, Serilog, and OpenTelemetry.
- `apps/worker`: tenant-aware, Redis-lock protected background job processor.
- `apps/web`: Next.js App Router operations console, BFF auth proxy, and public routes.
- `tests`: domain, API integration, and cross-tenant security tests.
- `infrastructure`: Docker, Nginx, and deployment configuration.
- `docs`: architecture, security, domain, lifecycle, privacy, and operating documentation.

## Implemented Capabilities

- Tenant resolution, membership, roles, JWT sessions, rotating refresh tokens, and append-only audits.
- Platform paths/subdomains, custom domains, live DNS TXT verification, Cloudflare SSL provisioning, and brand profiles.
- Competition lifecycle, rules, dynamic fields/page JSON, public entries, duplicate/risk checks, QR/source attribution, and analytics.
- Entry review, immutable draw snapshots, four-eye approval, secure winner/reserve selection, winner claims, and reserve promotion.
- S3 uploads, MIME signature validation, malware scanning, hashes, and signed downloads.
- Queued email/SMS, HMAC-signed webhooks, retries/dead letters, automatic closing, exports, reminders, and retention processing.
- Stripe checkout/webhooks and server-enforced subscription and plan limits.
- Draw certificate PDF generation and `/draw/{drawReference}` public integrity verification.
- API-backed dashboard screens for core operations, domains, brands, billing, integrations, and platform tenant/health views.

## Prerequisites

- .NET SDK 10.0.x
- Node.js 22.x and npm 11.x
- PostgreSQL 17
- Redis 7
- S3-compatible object storage; MinIO is included in local Docker Compose
- Docker for the complete local stack

## Local Setup

1. Copy `.env.example` to `.env` and replace development secrets.
2. Run `docker compose up --build` for PostgreSQL, Redis, MinIO, API, worker, web, and Nginx.
3. Open `http://localhost:8080`; MinIO Console is at `http://localhost:9001`.

To run processes directly:

```bash
dotnet tool restore
dotnet restore OneCompetitions.slnx
npm ci
dotnet run --project apps/api/OneCompetitions.Api
dotnet run --project apps/worker/OneCompetitions.Worker
npm --workspace apps/web run dev
```

Swagger is available from the API in Development at `/swagger`.

## Environment

`.env.example` documents local variables. `.env.production.example` is the production contract for database, Redis, trusted proxies, cryptographic keys, platform domains, S3, DNS/SSL, malware scanning, messaging, and Stripe. Store real values in a secret manager; never commit `.env` or `.env.production`.

Production rejects development DNS, SSL, storage, scanner, messaging, and lock providers at startup.

## Database Migrations

Migrations are under `apps/api/OneCompetitions.Infrastructure/Persistence/Migrations`, including `AddProductionOperations` and `CompleteProductionReadiness`.

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet ef migrations add <Name> \
  --project apps/api/OneCompetitions.Infrastructure \
  --startup-project apps/api/OneCompetitions.Api
```

The API image contains `/app/efbundle` for intentional production migration jobs.

## Verification

```bash
dotnet build OneCompetitions.slnx
dotnet test OneCompetitions.slnx
npm test
npm run lint
npm run build
npm audit --audit-level=high
```

## Demo Credentials

Development seeding creates `admin@onecompetitions.local`. Configure `DEV_ADMIN_PASSWORD`; the Development-only fallback is `DevelopmentOnly!ChangeMe123`.

## Deployment

Build the images in `infrastructure/docker`, configure secret-backed production environment values, run the migration profile, then start `docker-compose.production.yml`. TLS should terminate at the load balancer or ingress before Nginx.

```bash
docker compose -f docker-compose.production.yml --profile migration run --rm migrate
docker compose -f docker-compose.production.yml up -d
```

Current limitations: external staging deployment cannot be completed without environment credentials and a Docker-capable host; Kubernetes manifests, tenant deletion orchestration, social login, and high-volume queue partitioning are not included. Cloudflare, Stripe, SMTP, SMS, S3, and Turnstile require account-side configuration and staging validation. Next.js 16.2.10 currently carries a moderate transitive PostCSS advisory; the direct dependency is patched and the unsafe npm downgrade is intentionally not applied.
