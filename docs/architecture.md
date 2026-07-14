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

The MVP core adds:

- Competition CRUD, lifecycle, fields, rules, page JSON, publishing, and configuration versions.
- Public competition lookup and participant entry submission with idempotency, required fields, required consent enforcement, duplicate email/phone risk signals, and public entry references.
- Campaign sources, QR-code redirect tracking, and basic analytics.
- Entry review, approval, rejection, duplicate, and disqualification workflow.
- Draw preparation with immutable entry snapshots, pool hashes, approval, cryptographically secure winner and reserve selection without replacement, and replay protection.
- Winner claims with contact, acceptance, disqualification, reserve promotion, and delivery status.
- Export job records for CSV-style asynchronous export handoff.
- Data models for plans, subscriptions, feature flags, assets, retention policies, and webhooks.

Production integrations that remain intentionally abstracted or shallow include real DNS/SSL providers, object storage, queued notification delivery, PDF certificate generation, webhook delivery workers, retention workers, and payment providers.
