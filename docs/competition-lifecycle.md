# Competition Lifecycle

The standard-draw lifecycle supports draft creation, publication/version capture, scheduled/live operation, manual or automatic close, draw preparation/approval/execution, winner verification, completion, and archive/cancel states in the domain model.

Publishing validates rules, form configuration, dates, and limits and records an immutable configuration version. The worker closes elapsed scheduled/live/paused competitions under a distributed cycle lease and writes a system audit event. New entries validate current UTC time and state on the server.

Pause/resume, legal/client review transitions, reopening controls, and competition/page-level brand overrides need additional API/UI workflows.

## Editing Competitions

The competitions table has an Edit action, and each competition overview links to
`/dashboard/competitions/{id}/edit`. The form updates name, description, dates,
entry limits, winner/reserve counts, age/country restrictions, verification and
participant login methods through the tenant-authorised PATCH endpoint. URL slugs
remain unchanged so existing campaign links and QR destinations keep working.
Dates are edited in the browser's displayed time zone and submitted as UTC.

Published settings changes require confirmation in the dashboard, preserve the
previous configuration in a version record and create an audit event. These
settings take effect on save; content/media has a separate draft/publish editor.
Rules and entry fields remain available through their own editors. Saving settings
does not reopen a closed competition.

The API rejects settings changes for finalised competitions or competitions with
a non-cancelled/non-invalidated draw record. This protects prepared draw pools
even if the competition status is still Closed. The server continues enforcing
tenant and manager access; frontend controls do not replace those checks.
