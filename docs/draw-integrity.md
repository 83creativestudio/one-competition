# Draw Integrity

- Draw preparation freezes eligible entries into immutable snapshots.
- Canonical pool and configuration hashes detect later alteration.
- Optional separate approval enforces four-eye operation.
- Execution verifies competition state and pool hashes, acquires a distributed lock, and persists cryptographically secure selection without replacement in one transaction.
- Completed draws cannot execute twice; changed eligibility requires a new draw.
- Winner and reserve positions are preserved, including later disqualification/promotion history.
- The worker generates a PDF certificate containing audit-safe references and a public verification QR code.
- `/api/public/draws/{drawReference}/verification` recomputes the certificate SHA-256 hash and reveals no participant PII.
