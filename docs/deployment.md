# Deployment

Stage 1 deployment artifacts:

- `docker-compose.yml`
- `infrastructure/docker/api.Dockerfile`
- `infrastructure/docker/worker.Dockerfile`
- `infrastructure/docker/web.Dockerfile`
- `infrastructure/nginx/one-competitions.conf`
- `.github/workflows/ci.yml`

Production deployment must provide real secrets, HTTPS termination, PostgreSQL, Redis, object storage, and environment-specific platform domains.

Run migrations intentionally before serving production traffic.
