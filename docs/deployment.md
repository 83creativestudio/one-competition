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
