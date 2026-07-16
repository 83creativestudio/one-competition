# Authentication

ASP.NET Core Identity stores staff users and roles. Login requires confirmed email, enforces account lockout, creates a revocable session, and returns a short-lived JWT plus one-time rotating refresh token. Refresh use revokes the previous token; logout/session deletion revokes server-side state.

The Next.js BFF stores access/refresh tokens in HTTP-only, SameSite=Strict cookies and proxies API requests with bearer authentication. Mutations reject cross-origin requests. The profile cookie is display-only; authorization always occurs in the API.

Platform roles are distinct from tenant memberships. Tenant claims are issued only from active memberships, and middleware verifies tenant/host state for each request.

Tenant owners can invite, change, suspend, and remove team memberships. Invitations use hashed, expiring, one-time tokens and cannot remove or demote the final active owner. Password-reset tokens use ASP.NET Core Identity providers, reset revokes every active refresh session, and forgot-password responses do not reveal whether an account exists.

Authenticator-app MFA uses TOTP. Accounts with platform administration or tenant owner/administrator privileges must enroll before production login and cannot disable MFA while privileged. MFA setup requires the current password; enabling and disabling revoke active sessions. Development and test environments omit mandatory enrollment so seed accounts and automation remain usable.

Participant social login remains a future module.
