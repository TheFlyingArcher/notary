---
id: key-encryption-docs
title: Document the key storage exposure and fix
intent: encrypt-stored-private-keys
complexity: low
mode: autopilot
status: completed
depends_on:
  - plaintext-key-migration
created: 2026-10-15T00:00:00Z
run_id: run-specs-md-fire-planner-fad6e4-005
completed_at: 2026-10-16T00:23:18.146Z
---

# Work Item: Document the key storage exposure and fix

## Description

Document openly that earlier versions stored private keys unencrypted, what changed, and what operators should do.

## Acceptance Criteria

- [ ] A security note (README section or `docs/` page, plus `SECURITY.md` pointer if the repo has one) states: affected versions (those before this change), what was exposed (all CA and certificate private keys readable from the database or backups), and that the fix encrypts keys with PBES2 / PBKDF2-HMAC-SHA256 / AES-256-CBC
- [ ] Recommends rotating CA keys (and re-issuing certificates) for any deployment whose database or backups may have been accessible, and treating older backups as containing plaintext keys
- [ ] Documents `NOTARY_APP_KEY`: required, at least 16 characters, keep it secret and backed up, changing it makes stored keys unreadable (no re-key command yet)
- [ ] Describes the startup migration and the log lines it writes
- [ ] No private keys, passwords or real hostnames in the text

## Technical Notes

Find where configuration is already documented (README, `docs/`, setup scripts) and extend it rather than adding a parallel file.

## Dependencies

- plaintext-key-migration
