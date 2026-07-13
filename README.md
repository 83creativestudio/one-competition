# ONE. Competitions

ONE. Competitions is a multi-tenant SaaS platform for competition and giveaway management. Stage 1 implements the deployable foundation only: clean backend architecture, PostgreSQL persistence, Identity-based dashboard authentication, tenant memberships, tenant resolution, authorization policies, audit logging, health checks, Docker scaffolding, CI, and a minimal Next.js shell.

## Architecture

- `apps/api`: ASP.NET Core API using Clean Architecture projects.
- `apps/worker`: .NET Worker Service placeholder for tenant-aware background jobs.
- `apps/web`: Next.js App Router frontend shell.
- `tests`: .NET unit, integration, and security tests.
- `docs`: architecture and operating documentation.

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

The initial migration is `InitialFoundation` in `apps/api/OneCompetitions.Infrastructure/Persistence/Migrations`.

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
