---
id: app-key-validation
title: Validate NOTARY_APP_KEY at startup
intent: encrypt-stored-private-keys
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-15T00:00:00Z
run_id: run-specs-md-fire-planner-fad6e4-003
completed_at: 2026-10-16T00:16:49.384Z
---

# Work Item: Validate NOTARY_APP_KEY at startup

## Description

`NOTARY_APP_KEY` is the passphrase for every stored private key but is never validated. Fail startup when it is missing or weak.

## Acceptance Criteria

- [ ] `NotaryConfiguration.Validate()` throws `InvalidOperationException` when `ApplicationKey` is null, empty, whitespace, or shorter than 16 characters
- [ ] The message names `NOTARY_APP_KEY` and the minimum length, and never includes the value
- [ ] Startup calls `Validate()` (confirm the existing call site) so the app does not boot with a bad key
- [ ] Unit tests: null, empty, whitespace, 15 characters rejected; 16 characters accepted
- [ ] Existing tests and any test or sample configuration that sets a short key are updated

## Technical Notes

`src/library/core/Configuration/NotaryConfiguration.cs`, next to the CRL endpoint check. Check `src/test` and `appsettings.json`/Dockerfile/setup scripts for short or unset keys.

## Dependencies

(none)
