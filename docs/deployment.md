# Deployment

Artifacts:

- `docker-compose.yml`: local PostgreSQL, Redis, MinIO, API, worker, web, and Nginx.
- `docker-compose.production.yml`: immutable API/worker/web images and Nginx, with a migration profile.
- `infrastructure/docker/*.Dockerfile`: non-root runtime images; API/worker include fonts for certificates.
- `infrastructure/nginx/one-competitions.conf`: host-preserving reverse proxy.
- `.github/workflows/ci.yml`: build, tests, migration drift, dependency gate, web checks, and Compose validation.

Production must supply managed PostgreSQL, Redis, S3, malware scanning, SMTP/SMS, Cloudflare Custom Hostnames, Stripe credentials/prices, trusted proxy ranges, platform domains, and cryptographic keys. Use `.env.production.example` as the configuration contract and store values in a secret manager.

Run `/app/efbundle` as a one-off release job before new application images. Use `/health/live` for process health and `/health/ready` for database, Redis, and object-storage readiness. TLS terminates at the ingress/load balancer.

Docker was unavailable on the implementation workstation. CI or a Docker-capable staging host must execute image builds, Compose startup, health probes, migration rollback rehearsal, and external provider smoke tests before production release.

## Staging Gate

1. Build immutable API, worker, and web images from the same commit and scan them.
2. Provision isolated PostgreSQL, Redis, S3 bucket, Cloudflare zone/custom-hostname access, Stripe test mode, SMTP sandbox, and SMS sandbox credentials.
3. Load `.env.production.example` through the staging secret manager. Keep all production provider guards enabled; do not use a Development environment or development providers.
4. Back up PostgreSQL, run `/app/efbundle`, start the stack, then verify `/health/live` and `/health/ready` through the public ingress.
5. Exercise tenant host/path isolation, invitation plus MFA, password reset, Turnstile entry, email/SMS verification, upload scan/download, competition close, draw concurrency, winner SMS/email, export, privacy deletion, Stripe checkout/webhook/cancellation, and webhook retry/dead-letter behavior.
6. Confirm Redis lock ownership, S3 tenant prefixes/lifecycle, Cloudflare certificate activation, queue delivery logs, OpenTelemetry correlation fields, and that logs contain no participant PII or secrets.
7. Rehearse application rollback to the prior images. Database rollback must use a reviewed forward-fix migration unless the release migration is explicitly proven reversible against a restored staging backup.

Record evidence for image digests, migration ID, health probes, provider event IDs, test tenant IDs, and rollback duration before approving production.

After account-side configuration, run `infrastructure/scripts/validate-staging.sh` with `STAGING_BASE_URL`, `STAGING_TENANT_SLUG`, and `STAGING_COMPETITION_SLUG`. Run the k6 scenario documented in `tests/load/README.md` separately so load data cannot be sent accidentally by the smoke script.

## Dependency Advisory

The direct PostCSS dependency is pinned to `8.5.19`. Next.js `16.2.10` embeds PostCSS `8.4.31`, which npm reports under GHSA-qx2v-qp2m-jg93. `npm audit fix --force` incorrectly proposes Next 9 and must not be used. Keep `npm audit --audit-level=high` blocking CI, monitor the Next release line, and upgrade once Next publishes a compatible patched transitive dependency.
