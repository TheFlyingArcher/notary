---
run: run-specs-md-fire-planner-fad6e4-003
work_item: app-key-validation
intent: encrypt-stored-private-keys
mode: confirm
checkpoint: plan
approved_at: 2026-10-16T00:00:00Z
---

# Implementation Plan: Validate NOTARY_APP_KEY at startup

## Approach

Add a check to `NotaryConfiguration.Validate()` (already called from `src/web/Program.cs:21` before the container is built) that throws `InvalidOperationException` when `ApplicationKey` is null, empty, whitespace, or shorter than 16 characters. The message names `NOTARY_APP_KEY` and the minimum, and never echoes the value. The minimum is a public constant so the docs and tests share it.

The key check goes first in `Validate()`, since the other setting (CRL endpoint) is optional.

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/core/Configuration/NotaryConfiguration.cs` | `MinimumApplicationKeyLength = 16` constant; key check in `Validate()` |
| `src/test/NotaryConfigurationTest.cs` | Existing CRL-endpoint tests set a valid key (they call `Validate()`); new tests: null, empty, whitespace, 15 characters rejected; 16 characters accepted; message names `NOTARY_APP_KEY` and does not contain the value |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/NotaryConfigurationTest.cs` | Rejection of missing/short keys, acceptance at the minimum, no key leakage in the message |

## Technical Details

A repo-wide search found no sample or deployment configuration that sets `NOTARY_APP_KEY` (no appsettings, Dockerfile, compose or setup script mentions it), so nothing else needs updating. Operators who run with a short or unset key will now see the app refuse to start with a clear message; that is the intended fail-closed behavior, and the docs item will describe it. `AsymmetricKeyService` tests already use a 36-character key.

Length counts characters, not bytes or entropy; the docs recommend a long random value.

---
