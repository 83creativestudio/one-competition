# Domain Routing

Stage 2 includes the `TenantDomain` data model, tenant dashboard APIs, development DNS verification, and middleware support for active custom-domain records.

Supported foundation routing:

- `https://competitions.local/c/one-digital`
- `https://one-digital.competitions.local`
- Active `TenantDomain` records for future custom domains.

Development seed data creates platform subdomain records for example tenants.

Implemented dashboard endpoints:

- `GET /api/domains`
- `POST /api/domains`
- `GET /api/domains/{id}`
- `POST /api/domains/{id}/verify`
- `POST /api/domains/{id}/set-primary`
- `DELETE /api/domains/{id}`

The first verification provider is intentionally development-only. It verifies `.test`, `.local`, or hostnames containing `verified` and throws if used outside Development or Testing. Real DNS and SSL providers remain deferred.
