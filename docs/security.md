# Security

Stage 1 security controls:

- ASP.NET Core Identity password hashing.
- Account lockout settings.
- JWT bearer authentication.
- Explicit authorization policies.
- Tenant membership checks in application services.
- EF Core query filters for tenant-scoped records.
- Unknown-host rejection.
- Secure headers including CSP, frame denial, and nosniff.
- Append-only audit event protection.
- Cross-tenant integration and security tests.

Never log passwords, OAuth secrets, complete verification codes, payment data, or unmasked participant personal data.

Production must use HTTPS, real secrets, secure cookie domains, managed secret storage, and provider-specific credential encryption.
