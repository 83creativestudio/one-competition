# ONE. Competitions

ONE. Competitions is a multi-tenant SaaS platform for competition and giveaway management. The current implementation includes the deployable foundation, domain and branding capabilities, and a compact MVP core for standard-draw competitions: competition management, fields, rules, page JSON, public entry submission, duplicate email/phone risk scoring, QR/source tracking, analytics overview, entry review, immutable draw snapshots, cryptographically secure winner/reserve selection, winner workflow, export job records, Docker scaffolding, CI, and a Next.js route shell.

## Architecture

- `apps/api`: ASP.NET Core API using Clean Architecture projects.
- `apps/worker`: .NET Worker Service placeholder for tenant-aware background jobs.
- `apps/web`: Next.js App Router frontend shell.
- `tests`: .NET unit, integration, and security tests.
- `docs`: architecture and operating documentation.

## Implemented APIs

- Tenant domains: list, create, verify, set primary, delete.
- Brand profiles: list, create, get, update, delete.
- Public theme: subdomain/custom-domain route and platform path route.
- Competitions: create, update, publish, close, versions.
- Fields, rules, and page JSON.
- Public competition lookup and entry submission.
- Entries: list, approve, reject, duplicate, disqualify.
- Campaign sources, QR codes, QR redirect, analytics overview.
- Draws: prepare, approve, execute, results.
- Winners: contact, accept, disqualify, deliver prize.
- Exports: create/list export jobs.

## Prerequisites

- .NET SDK 10.0.x
- Node.js 22.x and npm 11.x
- PostgreSQL 17 for local API execution
- Redis 7 for later queue/cache work
- Docker is optional for local services; the current machine used for this implementation did not have Docker CLI installed.

## Local Setup

1. Copy `.env.example` to `.env` and replace secrets.
2. Start PostgreSQL and Redis, or use `docker compose up postgres redis` when Docker is available.
3. Restore backend packages: `dotnet restore OneCompetitions.slnx`.
4. Restore frontend packages: `npm ci`.
5. Apply migrations by running the API in development; the development seeder calls `Database.Migrate()`.

## Environment Variables

Required variables are documented in `.env.example`. Production must provide `JWT_SIGNING_KEY`, `DATABASE_CONNECTION_STRING`, platform domains, object storage settings, provider credentials, and encryption keys through secret management.

## Database Migrations

Migrations are in `apps/api/OneCompetitions.Infrastructure/Persistence/Migrations`:

- `InitialFoundation`
- `AddDomainsAndBranding`
- `AddCompetitionMvpCore`

Generate future migrations with the repo-local EF tool:

```bash
dotnet tool restore
dotnet dotnet-ef migrations add <Name> --project apps/api/OneCompetitions.Infrastructure/OneCompetitions.Infrastructure.csproj --startup-project apps/api/OneCompetitions.Api/OneCompetitions.Api.csproj --output-dir Persistence/Migrations
```

## Running the API

```bash
export JWT_SIGNING_KEY=replace-with-at-least-32-random-characters
export DATABASE_CONNECTION_STRING="Host=localhost;Port=5432;Database=one_competitions;Username=one;Password=one_dev_password"
dotnet run --project apps/api/OneCompetitions.Api/OneCompetitions.Api.csproj
```

Swagger is available in development at `/swagger`.

## Running the Worker

```bash
dotnet run --project apps/worker/OneCompetitions.Worker/OneCompetitions.Worker.csproj
```

## Running the Frontend

```bash
npm --workspace apps/web run dev
```

## Running Tests

```bash
dotnet test OneCompetitions.slnx
npm test
npm run lint
npm run build
```

## Demo Credentials

Development seeding creates `admin@onecompetitions.local`. Set `DEV_ADMIN_PASSWORD`; if omitted in development only, the fallback is `DevelopmentOnly!ChangeMe123`.

## Deployment Overview

Use the Dockerfiles in `infrastructure/docker` and route traffic through Nginx. Production must terminate HTTPS, set secure secrets, run PostgreSQL migrations intentionally, and disable all development-only defaults.
