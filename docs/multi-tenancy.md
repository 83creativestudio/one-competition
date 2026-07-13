# Multi-Tenancy

Every tenant-owned record includes `TenantId`. Stage 1 entities with tenant scope are `TenantUser`, `TenantDomain`, and `AuditEvent`.

Tenant resolution order implemented:

1. Platform path `/c/{tenantSlug}`.
2. Platform subdomain `{tenantSlug}.{PLATFORM_BASE_DOMAIN}`.
3. Authenticated user tenant claims.
4. Active tenant custom domain records.

The API uses a scoped `ITenantContext` populated by middleware. EF Core global filters protect tenant-scoped tables, while application services still perform explicit user-to-tenant access checks.

Unknown custom hosts are rejected before authorization. Suspended, cancelled, and archived tenants are rejected.
