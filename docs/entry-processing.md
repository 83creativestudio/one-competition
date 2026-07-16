# Entry Processing

Public submission resolves the tenant and published competition, validates lifecycle/time, CAPTCHA, idempotency, age/country rules, limits, required dynamic fields, and consent, then resolves or creates a tenant-specific participant inside a transaction. Answers are stored as structured records, fields marked sensitive are encrypted with the configured AES-GCM key, and public responses use generated entry references.

Email and phone verification records store hashed six-digit codes with expiry and attempt limits. Email/SMS messages are queued; an entry becomes eligible only after every required channel is verified. Confirmation notifications, analytics, and webhook events are queued after persistence so provider latency does not block the public response.

Turnstile is mandatory by default outside Development/Testing. The development CAPTCHA provider checks a fixed configured token and cannot start in production.
