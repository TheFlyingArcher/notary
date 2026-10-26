---
id: ca-data-migration
title: Move CA data from dev to the Atlas production database
intent: initial-ca-bootstrap
complexity: high
mode: validate
status: completed
depends_on: [ca-tls-bootstrap-runbook]
created: 2026-10-24T00:00:00Z
---

# Work Item: Move CA data from dev to the Atlas production database

## Description

Document and dry-run moving the CA, key, certificate, revocation and CRL data from the dev MongoDB to the production Atlas `notary` database.

## Acceptance Criteria

- [ ] Copies the whole dev `notary` database (all collections and indexes) with `mongodump --archive --gzip` and `mongorestore`, run from the `mongo` Docker image (no tools installed); commands given with dev as source and Atlas as target
- [ ] States that production `NOTARY_APP_KEY` must equal the key used in dev, and how to confirm: startup log `Private key migration: 0 migrated, 0 already encrypted... 0 failed`
- [ ] Handling of the dump file: it contains encrypted private keys; store it encrypted, restrict access, and securely delete it after use
- [ ] Atlas requirements: user with `readWrite` on `notary`, the Pi's IP on the allow-list, and the restoring machine's IP temporarily allowed (and removed after)
- [ ] Rollback procedure, and a check that a CA issued on dev can issue a certificate in production
- [ ] Dry-run procedure against a scratch Atlas database name (restore, run a local Notary against it, issue a test certificate, drop it) with a results table the owner fills in; the agent cannot run it (needs Atlas credentials)

## Technical Notes

Security-sensitive (key material). No app changes planned; if the dump/restore turns out unsafe or incomplete, record the gap and open an issue for an import tool.

## Dependencies

- ca-tls-bootstrap-runbook
