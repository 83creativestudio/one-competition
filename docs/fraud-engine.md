# Fraud Engine

The rules engine evaluates duplicate email/phone, IP and device volume, one-minute submission velocity, disposable email domains, completion speed, and invalid phone patterns. Rules are tenant-configurable with JSON thresholds/domain lists, score impact, action, and enabled state. Signals retain evidence and feed risk levels and the auditable review queue.

Reviewers may approve, reject, mark duplicate, or disqualify entries with reasons. Shared IP only contributes a configurable signal and never blocks by default. A rule blocks only when its explicit action is `Block`.

Promotional-code reuse, uploaded-file hash matching, referral graph abuse, and external reputation providers remain tied to future competition modules.
