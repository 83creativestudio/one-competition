# Entry Processing

Public submission resolves the tenant and published competition, validates lifecycle/time, idempotency, required dynamic fields and consent, then resolves or creates a tenant-specific participant inside a transaction. Answers are stored as structured records and public responses use generated entry references.

Duplicate email/phone checks create risk signals and scores. Manual-approval campaigns enter review; eligible automatic campaigns are approved. Confirmation notification and webhook events are queued after persistence so provider latency does not block the public response.

Email verification tokens, CAPTCHA/Turnstile, geo/age policies, phone verification, and encrypted sensitive dynamic answers remain to be completed.
