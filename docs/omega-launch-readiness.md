# Omega broadcast launch readiness

Status: NOT APPROVED for real on-air competitions. Updated 2026-10-21.

Required registration methods: email, Google, Apple and Facebook. Draws may happen
live on air, in the production control room during a programme, or afterwards.
Unlimited lifetime entries is a commercial goal, not an infinite throughput
guarantee. A measured peak, resource budget and overload policy are required.

Advance through the following gates in order. Evidence from test doubles does
not constitute real-provider acceptance. Do not load-test the public deployment
or execute a real prize draw as part of automated rehearsal.

## 1. Production security - in progress

Local implementation evidence (not yet deployed): MFA re-enrollment protection,
MFA password-failure lockout, transactional refresh-token rotation, login/logout
origin checks and authenticated unknown-host rejection are implemented.
`System.Security.Cryptography.Pkcs` is pinned to 8.0.2, the patched 8.x version in
[Microsoft's advisory](https://github.com/advisories/GHSA-846f-f764-76mx).
Backend tests: 40 passed (1 domain, 4 security, 35 integration). Frontend tests:
7 passed; configuration/preflight-template tests: 4 passed. Backend and frontend builds,
frontend lint and the infrastructure transitive-package vulnerability check pass.
Concurrent refresh consumption still requires PostgreSQL provider-level testing;
the regression suite currently verifies sequential token replay using SQLite.

Run `node --env-file=.env.production infrastructure/scripts/omega-preflight.mjs`
with Node 22+ on the secured deployment workstation. It prints only failing key
names, never secret values. This is a configuration check, not provider validation
or permission to launch. Do not commit the environment file.

- Fix MFA secret re-disclosure, enforce password-attempt lockout on MFA setup,
  prevent refresh-token double consumption and add login/logout origin checks.
- Reject unknown hosts before authenticated tenant fallback, including requests
  that carry valid dashboard access tokens.
- Resolve dependency advisories and pass backend/frontend checks.
- Enroll every privileged operator in MFA before enabling Production.
- Validate session revocation, privilege changes, CSRF and tenant isolation.
- Review forwarded-proxy trust and preserve HTTPS through the entire proxy chain.
- Replace unsigned profile-cookie trust with validated server-side session data.
- Production must not use Development environment, development providers or seeds.

## 2. Registration and real delivery - awaiting configuration

New-account setup instructions and safe configuration examples are prepared in
[the provider setup guide](omega-provider-setup.md). Accounts and real-provider
validation remain pending; preparation is not account provisioning.

- Existing adapters must be configured with server-side credentials from a secret
  manager, never entered into source control or this document.
- Establish a production central authentication hostname and TLS, then register
  its exact callback URLs for Google, Apple and Facebook. Keep email available.
- Verify successful login, cancellation, denied consent, expired state, one-time
  completion, return-domain rejection and tenant separation on real devices.
- Configure SMTP/email delivery, authenticated sender domain, delivery monitoring
  and queue retries. Verify confirmation and password-reset delivery to real inboxes.
- Provider login is not proof of following, liking or sharing a social account.
- SMS is not currently selected as a launch requirement.

## 3. Entry protection - not accepted

- Configure real Turnstile and reject invalid/replayed tokens server-side.
- Test duplicates, idempotency, verification expiry, consent separation and age/
  country requirements using the approved competition rules.
- Tune rate limits for mobile carrier/shared IP users without removing protection.
- Validate configurable risk thresholds and the human review queue.

## 4. Hosting and recovery - awaiting providers and operational evidence

- Select object storage, real malware scanning and monitoring/alert destinations.
- Validate PostgreSQL/Redis readiness, durable private uploads and secret rotation.
- Schedule encrypted off-server backups; restore DB and media into an isolated
  environment and record measured recovery time and acceptable data loss.
- Test rollback with migrations and persistent data. Assign primary/on-call staff.
- Demonstrate queue failure alerts, dead-letter handling and provider timeouts.

## 5. TV capacity - not measured

- Agree audience estimate, scan-to-entry conversion and burst duration. Adopt an
  explicit initial target for testing, then scale based on evidence, not promises.
- Use a disposable environment with isolated DB/bucket, provider sandbox keys and
  synthetic participants. Never reuse a one-time CAPTCHA token across requests.
- Test simultaneous QR scans, public pages, submissions and operator dashboards;
  record accepted/rejected entries, p95/p99 latency, resource use and queue drain.
- Test overload, retry/idempotency and closing-boundary races. No silent entry loss.
- Record the maximum validated capacity and the operational headroom before launch.

## 6. Omega campaign setup - awaiting approved content

- Confirm brand/logo, actual prize, dates in Europe/Nicosia, eligibility, rules,
  privacy text, entry limits, verification policy, winners/reserves and approval roles.
- Publish only approved content; test the QR on actual broadcast graphics, phones
  and distances, with a short typed URL as fallback.
- Assign separate preparer, approver and operator; preserve immutable snapshots.
- Never publish test competitions as genuine prize offers.

## 7. Legal and privacy - external approval required

- Omega's authorised legal/privacy team must approve rules, lawful processing,
  separate optional marketing consent, minors handling, retention, winner contact
  and public announcement policy, plus any applicable broadcast obligations.
- Record approver, approved document version and date. This engineering checklist
  is not a legal opinion or legal approval.

## 8. Broadcast rehearsal and go/no-go - not performed

- Rehearse in a dedicated TEST campaign with synthetic entrants and no real prizes.
- Run scan -> register -> verify -> review -> close -> freeze -> approve -> draw ->
  winner contact -> reserve replacement -> certificate/public verification.
- Repeat for live-on-air, control-room and after-programme operation.
- Rehearse network/provider failure and operator handover; define who may stop
  the draw and what the presenter announces if the system is unavailable.
- Preserve evidence and obtain named technical, production and legal sign-off.
- Do not announce a launch date until all preceding gates pass.
