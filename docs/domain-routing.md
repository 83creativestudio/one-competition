# Domain Routing

Tenant resolution order is verified custom domain, platform subdomain, authenticated membership, then `/c/{tenantSlug}`. Unknown, deleted, or suspended hosts are rejected before controllers execute.

Supported records are platform paths/subdomains and custom subdomain, root, or `www` domains. Custom onboarding creates a random verification token and expects `one-competitions-verify={token}` at the hostname or `_one-competitions.{hostname}`.

Development verification accepts controlled local hostnames and cannot run outside Development/Testing. Production uses live TXT lookup and an idempotent Cloudflare Custom Hostnames provider that queries existing hostname/SSL state before provisioning. The worker retries pending/error domains and rechecks certificates approaching expiry.

Nginx preserves `Host` and forwarded headers. Production accepts forwarded values only from explicitly configured proxy networks/addresses.

## Reserved Platform Hosts

`PLATFORM_BASE_DOMAIN`, the configured authentication/QR/verification hosts, and
the platform subdomain namespace are system-managed. Customers cannot register
them as custom domains. Shared platform hosts resolve through authenticated
workspace selection or explicit `/c/{tenantSlug}` routes, not tenant-domain rows.
A legacy erroneous tenant-domain row must not claim or disable these shared hosts.
The worker excludes platform-owned names from customer DNS checks.

Real customer custom domains still require Active status; unknown, unverified and
suspended customer hosts are rejected even when a request carries valid credentials.
Legacy conflicting rows should be disconnected with an audit event and retained
for history, not marked verified merely to bypass the error.

## Platform Address In The Dashboard

`GET /api/domains/platform-address` returns the configured shared platform hostname,
its HTTPS URL and the authorized workspace's `/c/{slug}` URL. The Domains screen
displays this as a read-only, platform-managed address. It is computed from trusted
configuration and tenant context, not inserted into `TenantDomain` or assigned to
one customer. Platform-managed domains cannot be manually DNS-verified or deleted.
