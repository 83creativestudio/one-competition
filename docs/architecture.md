# Architecture

ONE. Competitions is a monorepo with separate API, worker, and web applications.

Stage 1 uses Clean Architecture for the backend:

- `OneCompetitions.Domain`: tenant, membership, domain, and audit entities.
- `OneCompetitions.Application`: role constants, permission policy names, tenant context interfaces, auth and tenant service contracts.
- `OneCompetitions.Contracts`: request and response DTOs returned by REST APIs.
- `OneCompetitions.Infrastructure`: EF Core, PostgreSQL/SQLite provider selection, ASP.NET Core Identity, seed data, auth service, tenant access service, and audit writer.
- `OneCompetitions.Api`: controllers, middleware, JWT auth, policies, Swagger, health endpoints, structured logging, and OpenTelemetry.

Dependencies flow inward. Controllers call application interfaces and do not query EF directly.

Stage 2 adds tenant domain management, development domain verification, brand profiles, and public theme resolution.

Competitions, entries, QR codes, fraud, draw execution, winners, billing, webhooks, and storage are intentionally deferred until their stages.
