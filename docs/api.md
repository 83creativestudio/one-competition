# API

Implemented endpoints:

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

Competitions:

- `GET /api/competitions`
- `POST /api/competitions`
- `GET /api/competitions/{id}`
- `PATCH /api/competitions/{id}`
- `POST /api/competitions/{id}/publish`
- `POST /api/competitions/{id}/close`
- `GET /api/competitions/{id}/versions`
- `GET /api/competitions/{id}/fields`
- `POST /api/competitions/{id}/fields`
- `PATCH /api/competitions/{id}/fields/{fieldId}`
- `DELETE /api/competitions/{id}/fields/{fieldId}`
- `POST /api/competitions/{id}/fields/reorder`
- `POST /api/competitions/{id}/rules`
- `PUT /api/competitions/{id}/page`

Public:

- `GET /api/public/competitions/{slug}`
- `POST /api/public/competitions/{slug}/entries`
- `GET /api/public/entries/{reference}/status`

Entries:

- `GET /api/competitions/{id}/entries`
- `GET /api/competitions/{id}/entries/{entryId}`
- `POST /api/competitions/{id}/entries/{entryId}/approve`
- `POST /api/competitions/{id}/entries/{entryId}/reject`
- `POST /api/competitions/{id}/entries/{entryId}/mark-duplicate`
- `POST /api/competitions/{id}/entries/{entryId}/disqualify`

Campaigns, QR, and analytics:

- `GET /api/competitions/{id}/sources`
- `POST /api/competitions/{id}/sources`
- `GET /api/competitions/{id}/qr-codes`
- `POST /api/competitions/{id}/qr-codes`
- `GET /q/{shortCode}`
- `GET /api/competitions/{id}/analytics/overview`

Draws, winners, and exports:

- `GET /api/competitions/{id}/draws`
- `POST /api/competitions/{id}/draws/prepare`
- `POST /api/competitions/{id}/draws/{drawId}/approve`
- `POST /api/competitions/{id}/draws/{drawId}/execute`
- `GET /api/competitions/{id}/draws/{drawId}/results`
- `GET /api/competitions/{id}/winners`
- `POST /api/competitions/{id}/winners/{winnerId}/contact`
- `POST /api/competitions/{id}/winners/{winnerId}/accept`
- `POST /api/competitions/{id}/winners/{winnerId}/disqualify`
- `POST /api/competitions/{id}/winners/{winnerId}/deliver-prize`
- `GET /api/competitions/{id}/exports`
- `POST /api/competitions/{id}/exports`

Platform:

- `GET /api/platform/tenants`

Health:

- `GET /health/live`
- `GET /health/ready`

Swagger is enabled only in development.
