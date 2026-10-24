# Entry Processing

Public submission resolves the tenant and published competition, validates lifecycle/time, CAPTCHA, idempotency, age/country rules, limits, required dynamic fields, and consent, then resolves or creates a tenant-specific participant inside a transaction. Answers are stored as structured records, fields marked sensitive are encrypted with the configured AES-GCM key, and public responses use generated entry references.

Email and phone verification records store hashed six-digit codes with expiry and attempt limits. Email/SMS messages are queued; an entry becomes eligible only after every required channel is verified. Confirmation notifications, analytics, and webhook events are queued after persistence so provider latency does not block the public response.

Turnstile is mandatory by default outside Development/Testing. The development CAPTCHA provider checks a fixed configured token and cannot start in production.
# Public Eligibility Inputs

The public entry form adds an explicit country-of-residence selector when a
competition restricts countries but its custom fields omit a visible country
input. Options come from the competition's allowed ISO country codes; no country
is selected automatically or inferred from a telephone number. A required date
of birth is likewise collected when a minimum age is configured. Existing typed
Country and DateOfBirth fields are mapped by their configured field keys.
Server-side eligibility validation remains authoritative.
