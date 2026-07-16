# Multi-Tenancy

Every tenant-owned aggregate stores `TenantId`. A trusted scoped context is populated from an active domain/path or a verified authenticated membership; browser-supplied tenant identifiers are not trusted.

Isolation is layered: authentication claims, middleware host/membership validation, application-service role checks, explicit tenant predicates for system work, EF global query filters, tenant-prefixed object keys, tenant-aware lock/dedup keys, and security tests. Platform administrators may bypass tenant membership only through explicit platform authorization paths.

Background jobs use `IgnoreQueryFilters()` only when scanning all tenants and then retain explicit `TenantId` predicates/ownership on resulting operations. Public endpoints must resolve tenant from the request host or platform path.
