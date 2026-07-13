# Draw Integrity

Draw preparation, immutable snapshots, secure random winner selection, reserve winners, and draw certificates are deferred to Stage 7 and Stage 8.

Stage 1 foundations relevant to draw integrity:

- Role constants include `DrawOperator` and `Auditor`.
- Audit events are append-only.
- Worker project exists for future draw certificate generation.
- Database migrations are established.

Production draw logic must use server-side cryptographically secure randomness and distributed locking.
