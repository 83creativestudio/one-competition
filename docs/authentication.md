# Authentication

ASP.NET Core Identity stores staff users and roles. Login requires confirmed email, enforces account lockout, creates a revocable session, and returns a short-lived JWT plus one-time rotating refresh token. Refresh use revokes the previous token; logout/session deletion revokes server-side state.

The Next.js BFF stores access/refresh tokens in HTTP-only, SameSite=Strict cookies and proxies API requests with bearer authentication. Mutations reject cross-origin requests. The profile cookie is display-only; authorization always occurs in the API.

Platform roles are distinct from tenant memberships. Tenant claims are issued only from active memberships, and middleware verifies tenant/host state for each request. MFA, password reset, user invitation, and participant social-login flows remain to be completed.
