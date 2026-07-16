# Fraud Engine

The MVP detects duplicate tenant/competition email and phone identities, records entry risk signals, computes a basic score/level, and exposes an auditable review queue. Reviewers may approve, reject, mark duplicate, or disqualify entries with reasons. Shared IP alone is not an automatic block.

The schema supports configurable fraud rules. Device/IP velocity, disposable email, promotional code, file hash, referral abuse, and per-competition threshold configuration remain to be implemented.
