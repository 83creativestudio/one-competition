# Architecture

ONE. Competitions is a monorepo with independently deployable API, worker, and web applications.

The backend follows Clean Architecture:

- `OneCompetitions.Domain`: tenant, competition, entry, draw, asset, billing, queue, webhook, and audit entities.
- `OneCompetitions.Application`: permissions, trusted tenant context, use-case contracts, and provider abstractions.
- `OneCompetitions.Contracts`: REST request/response DTOs; EF entities are never returned directly.
- `OneCompetitions.Infrastructure`: EF Core/PostgreSQL, Identity, business services, S3, Redis, DNS, Cloudflare, Stripe, messaging, webhooks, PDF generation, and health checks.
- `OneCompetitions.Api`: controllers, middleware, JWT/policies, rate limits, OpenAPI, Serilog, and OpenTelemetry.

Dependencies flow inward. Controllers call application interfaces and contain no business logic.

The worker executes an idempotent cycle under a Redis lease. It processes automatic closing, DNS/SSL checks, email/SMS, exports, webhooks, draw certificates, winner reminders, retention anonymisation, and expired export cleanup. Durable records carry attempt, availability, and lease fields so abandoned work can resume.

The web application uses a same-origin backend-for-frontend proxy. Access and rotating refresh tokens remain in HTTP-only cookies; React Query powers authenticated dashboard reads and mutations. Public campaign and draw-verification routes use server rendering.

PostgreSQL is authoritative relational storage, Redis coordinates distributed work, and S3-compatible storage holds uploads, exports, and certificates. All storage keys and cache/lock keys are tenant-aware where ownership applies.
