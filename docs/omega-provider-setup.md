# Omega provider account setup

Prepared 2026-10-21. This is a setup pack, not a completed integration or approval
to launch. No paid accounts, credentials, cloud resources or DNS records have been
created. Account owners must accept provider terms, enable billing when necessary
and approve spending. Confirm ownership and data-processing arrangements first.

## Proposed accounts

| Purpose | Proposed service | Account owner action |
| --- | --- | --- |
| Transactional email | Amazon SES, eu-central-1 (Frankfurt) | Create business AWS account, MFA and billing alerts |
| Private files | Amazon S3, eu-central-1 | Create separate private production/test buckets |
| Bot protection | Cloudflare Turnstile | Create production widget and separate test widget |
| Google registration | Google Cloud OAuth web client | Create business project and consent configuration |
| Apple registration | Sign in with Apple | Check eligibility, developer membership and primary app association |
| Facebook registration | Meta developer application | Create business-owned app and complete launch requirements |

These choices fit the existing SMTP/S3/identity adapters; they are not mandatory
vendor dependencies. EU storage selection alone does not establish legal compliance.
SMS, malware scanning, off-server backup and monitoring still need separate setup;
do not use Development providers as substitutes for production services.

## 1. Ownership and secret handling

Use business-owned accounts, two authorised administrators, individual logins,
MFA and recovery codes in the organisation's password manager. Do not share root
accounts or grant the app AWS administrator permissions. Use separate production
and test identities. Enable spend alerts before provisioning resources.

Use `infrastructure/deployment/omega/.env.providers.example` as an overlay on the
complete production configuration. It intentionally leaves secrets blank. Store
real secrets outside Git, preferably in a secret manager; restrict any generated
environment file to its deployment owner (mode 600). Do not paste secrets in chat.
Never replace existing JWT/encryption keys casually: rotation affects sessions and
stored encrypted data. A `.p8` Apple key belongs in secret storage, not the repo.

## 2. Domain and callback registration

Proposed central authentication host: `auth.competition.onedigital.com.cy`.
It is NOT confirmed provisioned. Configure DNS to the deployment gateway, a valid
TLS certificate and host-based proxy routing to the API before provider testing.
Set `PLATFORM_AUTH_DOMAIN=https://auth.competition.onedigital.com.cy` identically in
API and worker configuration. Preserve HTTPS forwarded headers through both proxies.

Register these exact URLs, with no trailing slash:

| Provider | Callback |
| --- | --- |
| Google | `https://auth.competition.onedigital.com.cy/api/participant-auth/google/callback` |
| Apple | `https://auth.competition.onedigital.com.cy/api/participant-auth/apple/callback` |
| Facebook | `https://auth.competition.onedigital.com.cy/api/participant-auth/facebook/callback` |

These paths match `ParticipantAuthController` and `SocialAuthService.CallbackUri`.
Apple returns a cross-site form POST; permit it to the callback, without adding a
dashboard-session CSRF requirement to that endpoint. OAuth state/nonce validation
still applies. Do not add customer domains as provider callbacks or use wildcard
callback URLs. Participant return URLs must remain tenant-validated.

## 3. AWS SES email

1. Create the business AWS account and select Frankfurt for SES.
2. Verify `competition.onedigital.com.cy` as the sending-domain identity. Publish
   the exact DKIM and any MAIL FROM records supplied by SES; do not invent values
   or overwrite existing domain email records. Agree DMARC policy with the mail owner.
3. Use proposed sender `no-reply@competition.onedigital.com.cy`; confirm a monitored
   support/reply address before launch. Outgoing messages are transactional only.
4. Request production access and a sending quota covering the tested entry peak.
   SES sandbox accounts cannot deliver freely to arbitrary entrants. Configure
   bounce/complaint feedback and suppression handling before enabling real traffic.
5. Generate region-specific SES SMTP credentials. Set `SMTP_HOST` to
   `email-smtp.eu-central-1.amazonaws.com`, port 587 and TLS enabled. SMTP passwords
   differ from AWS API secret keys. Keep storage credentials separate.
6. Test queue -> delivery -> verification using Gmail, Outlook and Apple relay
   addresses. Verify expiry/retry behaviour and alerts when delivery fails.

Sources: [SES production access](https://docs.aws.amazon.com/ses/latest/dg/request-production-access.html),
[SMTP credentials](https://docs.aws.amazon.com/ses/latest/dg/smtp-credentials.html),
[regional endpoints](https://docs.aws.amazon.com/general/latest/gr/ses.html).

## 4. Private S3 storage

Create unique production and test bucket names in Frankfurt. Enable Block Public
Access, bucket-owner-enforced object ownership, default encryption and versioning.
Do not enable public bucket hosting. Set lifecycle rules only after agreeing
retention; application retention does not automatically remove older S3 versions.
Backups must include the database, objects and a tested recovery procedure.

Replace `REPLACE_WITH_BUCKET_NAME` in `s3-runtime-policy.example.json` before applying
it to a dedicated runtime identity. Its object permissions cover the current
`tenant/` storage namespace and `system/health/` readiness checks; no bucket
administration, IAM management or all-buckets access is granted. This policy is
not a backup policy and deliberately cannot enumerate/delete object versions.
If SSE-KMS is selected, separately review required KMS permissions.

Apply `s3-cors.example.json` only to the intended bucket. It permits the exact
production frontend origin, not every website. Add custom tenant origins only
after verification; do not default to `*`. CORS is not access control: objects stay
private and uploads/downloads use short-lived signed requests.
[AWS CORS reference](https://docs.aws.amazon.com/AmazonS3/latest/userguide/ManageCorsUsing.html).

The existing malware scanner requires a real HTTP service implementing the app's
scanner contract. S3 alone does not satisfy it. Validate malicious-file rejection,
clean file publication, private downloads and storage health with actual credentials.

## 5. Google

Create a Google Cloud project and configure the OAuth consent/branding, support
contact and authorised domain `onedigital.com.cy`. Create a Web application OAuth
client with the exact Google callback above. The existing adapter requests only
`openid email profile`. Put its client ID/secret into server-side configuration.
Use test users first, then complete Google's current publishing/verification
requirements shown for the app before allowing the general audience.
[Google web-server OAuth](https://developers.google.com/identity/protocols/oauth2/web-server).

## 6. Apple: eligibility gate first

Apple's current documentation requires web sign-in to be associated with an
eligible primary Apple app. Confirm that the business has the qualifying app and
developer account before promising Apple availability. Do not create a fictitious
app or assume a website-only account will meet Apple's requirements.

When eligible, create a Services ID, associate the primary app, register the auth
domain/Apple return URL above, and create a Sign in with Apple key. Store Services
ID as `APPLE_CLIENT_ID`, plus team ID, key ID and the complete private PEM as
`APPLE_PRIVATE_KEY`. This adapter generates its client-secret JWT server-side;
it does not use a static `APPLE_CLIENT_SECRET` setting.

Register the actual outbound sender/domain for Apple's private email relay and
validate authenticated delivery. Test Hide My Email and returning users; collect
missing names through the entry form rather than assuming Apple always supplies them.
Sources: [web setup](https://developer.apple.com/help/account/capabilities/configure-sign-in-with-apple-for-the-web),
[environment requirements](https://developer.apple.com/documentation/signinwithapple/configuring-your-environment-for-sign-in-with-apple),
[private relay](https://developer.apple.com/help/account/capabilities/configure-private-email-relay-service/).

## 7. Facebook

Create a business-owned Meta app for Facebook authentication. Configure its exact
callback above and real public privacy/deletion-information URLs. Do not submit
placeholder policy URLs. Store App ID/secret as `FACEBOOK_CLIENT_ID` and
`FACEBOOK_CLIENT_SECRET`. Confirm a supported Graph API version in the app dashboard.
Only `public_profile,email` are requested by the existing adapter.

Test with assigned app testers, then complete Meta's current access, business,
review and live-mode requirements shown for this app. Do not assume tester login
means public launch approval. Meta documentation could not be fetched during this
preparation (HTTP 429), so dashboard requirements need reconfirmation.
[Meta login reference](https://developers.facebook.com/docs/facebook-login/guides/advanced/manual-flow/).

Facebook may omit email; the app does not treat its profile email as verified.
Test separate email collection/verification. Social login does not verify a Page
follow, post like, share or Instagram action.

## 8. Turnstile and acceptance handover

Create the production widget for `competition.onedigital.com.cy`; configure the
site key and server secret. Use a separate test widget/environment. Verify widget
rendering, server validation, expiry and replay rejection.
[Turnstile setup](https://developers.cloudflare.com/turnstile/get-started/).

Run the configuration-only check on the secured deployment workstation:

```bash
node --env-file=/secure/path/.env.production infrastructure/scripts/omega-preflight.mjs
```

A pass only confirms configuration shape. Before launch, record successful real
email delivery, signed storage upload/download, scanner rejection, provider login
and cancellation, state/nonce/return-domain rejection, one-time code replay denial
and tenant separation. Test both mobile and desktop, including Apple form POST.
Keep providers disabled/unadvertised until those tests pass. Apple may remain a
launch blocker if the qualifying app is unavailable; any reduced launch scope
requires Omega's explicit agreement.
