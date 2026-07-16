# Security

Implemented controls include:

- ASP.NET Core Identity hashing, account lockout, confirmed email, JWT validation, rotating refresh sessions, and revocation.
- Explicit platform/tenant policies, service-level membership checks, tenant middleware, EF query filters, and cross-tenant tests.
- HTTPS/HSTS in production, CSP and secure headers, fixed-window rate limiting, strict same-origin BFF mutations, and HTTP-only cookies.
- One-hop forwarded headers restricted to configured proxy addresses/networks.
- Append-only audit events and sanitized production error responses/logging.
- Production startup rejection for development DNS, SSL, storage, scanner, messaging, or lock providers.
- S3 presigned URLs, tenant-prefixed keys, size/extension/MIME signature checks, malware scanning, and SHA-256 file hashes.
- AES-GCM encryption for webhook secrets, HMAC-SHA256 delivery signatures, retries/dead letters, and endpoint disablement.
- Public-network validation when webhook endpoints are created and delivered to reduce SSRF and DNS-rebinding exposure.
- Redis locks for draw execution and worker cycles; immutable snapshots and replay protection for completed draws.
- Stripe webhook signature validation and backend subscription/plan enforcement.

Never log passwords, provider secrets, verification codes, payment data, or normal unmasked participant PII. Production keys and credentials belong in a managed secret store.
