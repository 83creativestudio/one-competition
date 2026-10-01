# Authentication

ASP.NET Core Identity stores staff users and roles. Login requires confirmed email, enforces account lockout, creates a revocable session, and returns a short-lived JWT plus one-time rotating refresh token. Refresh use revokes the previous token; logout/session deletion revokes server-side state.

The Next.js BFF stores access/refresh tokens in HTTP-only, SameSite=Strict cookies and proxies API requests with bearer authentication. Mutations reject cross-origin requests. The profile cookie is display-only; authorization always occurs in the API.

Platform roles are distinct from tenant memberships. Tenant claims are issued only from active memberships, and middleware verifies tenant/host state for each request.

Tenant owners can invite, change, suspend, and remove team memberships. Invitations use hashed, expiring, one-time tokens and cannot remove or demote the final active owner. Password-reset tokens use ASP.NET Core Identity providers, reset revokes every active refresh session, and forgot-password responses do not reveal whether an account exists.

Authenticator-app MFA uses TOTP. Accounts with platform administration or tenant owner/administrator privileges must enroll before production login and cannot disable MFA while privileged. MFA setup requires the current password; enabling and disabling revoke active sessions. Development and test environments omit mandatory enrollment so seed accounts and automation remain usable.

## Participant social login

Public competitions can enable Email, Google, Apple, Facebook, X, and TikTok independently. All OAuth callbacks use the central `PLATFORM_AUTH_DOMAIN`; customer domains are return destinations, not provider callback URLs.

1. The campaign creates a ten-minute `AuthTransaction` containing hashed state, encrypted PKCE verifier and encrypted nonce.
2. The provider redirects to `/api/participant-auth/{provider}/callback` on the central auth host.
3. The API validates state, transaction expiry, provider, callback token, issuer/audience/nonce where the provider supplies OIDC, and the tenant-owned return URL.
4. The callback returns a two-minute, single-use completion code to the campaign domain.
5. The Next.js BFF exchanges that code and stores a 30-minute, single-use participant-session token in an HTTP-only SameSite cookie.
6. Entry submission binds the session to its tenant, competition, participant, and provider identity. Provider access and refresh tokens remain encrypted in the API database and never reach React.

Provider callback URLs to register are `https://{PLATFORM_AUTH_DOMAIN}/api/participant-auth/google/callback`, with the provider segment changed to `apple`, `facebook`, `x`, or `tiktok`. Apple uses `form_post`; the endpoint accepts both GET and form POST callbacks.

Provider application setup references:

- Google OpenID Connect: <https://developers.google.com/identity/openid-connect/openid-connect>
- Sign in with Apple web configuration: <https://developer.apple.com/documentation/signinwithapple/configuring-your-environment-for-sign-in-with-apple>
- Facebook Login: <https://developers.facebook.com/docs/facebook-login/>
- X OAuth 2.0 Authorization Code with PKCE: <https://docs.x.com/fundamentals/authentication/oauth-2-0/authorization-code>
- TikTok Login Kit for Web: <https://developers.tiktok.com/doc/login-kit-web>

Google uses OIDC plus PKCE. Apple uses its signed client-secret flow and OIDC nonce validation. Facebook uses OAuth state and Graph profile lookup. X uses OAuth 2.0 Authorization Code with PKCE. TikTok uses Login Kit for Web. Provider credentials are required before a competition enabling that provider can be published.

Social-action checks are capability-based. The current production adapter can verify X Follow and Like actions when the application has the required scopes and API access. Other provider actions may be displayed only as optional instructions; the API rejects them as required because it cannot prove completion. Social participation consent and marketing consent remain separate regardless of login method.
