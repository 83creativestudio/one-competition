# Entry Processing

Participant entry processing is deferred to Stage 4.

Stage 1 prepares:

- Trusted tenant context.
- Authenticated tenant user access.
- Append-only audit infrastructure.
- Database migration workflow.

Future entry submission must resolve tenant from hostname, validate competition state, enforce idempotency, save structured answers and consents, and queue notifications without blocking the public flow.
