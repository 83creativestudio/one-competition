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

Platform:

- `GET /api/platform/tenants`

Health:

- `GET /health/live`
- `GET /health/ready`

Swagger is enabled only in development.
