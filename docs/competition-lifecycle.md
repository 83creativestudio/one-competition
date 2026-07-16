# Competition Lifecycle

The standard-draw lifecycle supports draft creation, publication/version capture, scheduled/live operation, manual or automatic close, draw preparation/approval/execution, winner verification, completion, and archive/cancel states in the domain model.

Publishing validates rules, form configuration, dates, and limits and records an immutable configuration version. The worker closes elapsed scheduled/live/paused competitions under a distributed cycle lease and writes a system audit event. New entries validate current UTC time and state on the server.

Pause/resume, legal/client review transitions, reopening controls, and competition/page-level brand overrides need additional API/UI workflows.
