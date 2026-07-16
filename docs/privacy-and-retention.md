# Privacy And Retention

Participants and all associated identities, entries, answers, consents, assets, exports, and analytics are tenant-specific. Public APIs expose entry/draw references rather than database IDs. Competition participation and marketing consent remain separate records with accepted text hashes.

The worker applies tenant retention policies, anonymises expired participant profile/contact data, deletes expired export objects, and audits participant anonymisation. Draw snapshots, result references, and integrity hashes remain independent of mutable participant PII.

Self-service participant deletion/export request APIs, consent-withdrawal UI, tenant deletion orchestration, and competition-specific upload deletion schedules remain required before a full privacy operations release.
