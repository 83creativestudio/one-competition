# API

Stage 1 endpoints:

Authentication:

- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/sessions`
- `DELETE /api/auth/sessions/{id}`

Tenants:

- `GET /api/tenants/current`
- `GET /api/tenants/current/users`

Domains:

- `GET /api/domains`
- `POST /api/domains`
- `GET /api/domains/{id}`
- `POST /api/domains/{id}/verify`
- `POST /api/domains/{id}/set-primary`
- `DELETE /api/domains/{id}`

Branding:

- `GET /api/brands`
- `POST /api/brands`
- `GET /api/brands/{id}`
- `PATCH /api/brands/{id}`
- `DELETE /api/brands/{id}`
- `GET /api/public/theme`
- `GET /c/{tenantSlug}/api/public/theme`

Platform:

- `GET /api/platform/tenants`

Health:

- `GET /health/live`
- `GET /health/ready`

Swagger is enabled only in development.
