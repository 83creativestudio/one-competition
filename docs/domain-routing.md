# Domain Routing

Tenant resolution order is verified custom domain, platform subdomain, authenticated membership, then `/c/{tenantSlug}`. Unknown, deleted, or suspended hosts are rejected before controllers execute.

Supported records are platform paths/subdomains and custom subdomain, root, or `www` domains. Custom onboarding creates a random verification token and expects `one-competitions-verify={token}` at the hostname or `_one-competitions.{hostname}`.

Development verification accepts controlled local hostnames and cannot run outside Development/Testing. Production uses live TXT lookup and an idempotent Cloudflare Custom Hostnames provider that queries existing hostname/SSL state before provisioning. The worker retries pending/error domains and rechecks certificates approaching expiry.

Nginx preserves `Host` and forwarded headers. Production accepts forwarded values only from explicitly configured proxy networks/addresses.
