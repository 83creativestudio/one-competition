# Privacy And Retention

Participants and all associated identities, entries, answers, consents, assets, exports, and analytics are tenant-specific. Public APIs expose entry/draw references rather than database IDs. Competition participation and marketing consent remain separate records with accepted text hashes.

The worker applies tenant or competition retention policies, anonymises expired participant profile/contact data, deletes expired export and upload objects, and audits participant/asset retention actions. Draw snapshots, result references, and integrity hashes remain independent of mutable participant PII.

Public privacy requests support data export, deletion/anonymisation, and withdrawal of marketing email/SMS/phone/profiling consent. The service prevents account enumeration, sends a hashed 30-minute one-time authorization token, scopes every request to the resolved tenant, and returns short-lived signed download URLs for exports. Deletion removes uploaded objects and anonymises identity/profile fields while retaining competition/draw integrity records.

Tenant-wide account deletion orchestration and regulator-specific legal-hold workflows remain operational policy work.
