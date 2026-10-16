---
run: run-specs-md-fire-planner-fad6e4-004
work_item: plaintext-key-migration
intent: encrypt-stored-private-keys
generated: 2026-10-16T00:00:00Z
mode: validate
---

# Implementation Walkthrough: Re-encrypt existing keys at startup

## Summary

On startup the web app now brings every stored private key into the PBES2 format added in the previous run. Plaintext keys (written since commit `f9aadd0`) and keys in the older PBES1 3DES format (written before it) are decrypted if needed, re-encrypted with PBKDF2-HMAC-SHA256 and AES-256-CBC, checked by decrypting the result, and only then saved. Rows already in the new format are skipped, so the step is safe on every start. Rows that cannot be migrated are logged and left unchanged.

## Structure Overview

`IAsymmetricKeyService.MigrateStoredKeysAsync()` returns a `KeyMigrationResult(Migrated, AlreadyEncrypted, Failed)`. `Program.cs` calls it inside a scope right after `builder.Build()`. Inside `AsymmetricKeyService`: `IsCurrentFormat` (no decryption), `ReadLegacyKey` (BouncyCastle's general PEM reader, as the old loader did) and the strict writer/reader from run 002.

## Files Changed

### Created

| File | Purpose |
|------|---------|
| `src/library/core/Contract/KeyMigrationResult.cs` | Result record with the three counts |
| `src/test/AsymmetricKeyMigrationTest.cs` | 9 tests for migration |

### Modified

| File | Changes |
|------|---------|
| `src/library/service/AsymmetricKeyService.cs` | Migration method, format detection, legacy reader |
| `src/library/core/Interface/Service/IAsymmetricKeyService.cs` | New `MigrateStoredKeysAsync` member |
| `src/web/Program.cs` | Run the migration at startup |
| `src/test/InMemoryFakes.cs` | `FakeKeyService` implements the new member |

## Key Implementation Details

### 1. Three stored formats

| Stored form | Action |
|-------------|--------|
| PBES2 / PBKDF2-HMAC-SHA256 / AES-256-CBC | Skipped |
| PBES1 SHA-1 / 3DES (before `f9aadd0`) | Decrypted with `NOTARY_APP_KEY`, upgraded |
| Plaintext `PRIVATE KEY` (`f9aadd0` onward) | Upgraded |

### 2. Safe replace

The new value is decrypted again and compared with the original key before the row is saved, so a row can never be replaced by something unreadable. The save is the repository's replace-by-slug upsert, the same call used for every other save; `Updated` and `UpdatedBySlug = "key-migration"` are set.

### 3. Failure behavior

A bad row (garbage, empty, a legacy row under a different passphrase) is counted as failed, logged with its slug and the exception type and message, and left byte-identical. It then fails to load, because the loader refuses anything that is not the current format. A failure to read the key list at all stops startup.

## Security Considerations

| Concern | Approach |
|---------|----------|
| Plaintext on disk | Never written; the migration only writes the new encrypted form |
| Data loss | Verify-before-replace; failed rows untouched |
| Old backups | A warning is logged when any key was migrated: backups from before still hold the old form |
| Log content | Slug and exception details only |

## Performance Considerations

| Requirement | Implementation |
|-------------|----------------|
| Startup cost | Two PBKDF2 runs (600,000 iterations) per migrated key; about 0.6 s per key on the dev machine, several times that on a Raspberry Pi. Already-current keys cost nothing |

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Handle legacy 3DES rows | Yes (approved at the plan checkpoint) | Weak encryption (SHA-1, 3DES) and the strict loader would otherwise strand them |
| Run at web startup | Yes | Operators cannot forget it |
| Failed rows | Log and continue | One bad row must not take the CA offline |

## Deviations from Plan

None

## Dependencies Added

(none)

## How to Verify

1. **Run the tests**
   ```bash
   dotnet test ./src/test/
   ```
   Expected: 352 passed.

2. **Try it against a development database (Visual Studio UAT)**
   Use a throwaway database only. Start the web app with a `NOTARY_APP_KEY` of at least 16 characters on a database containing keys created by the previous build (plaintext) and, if available, by a build from before `f9aadd0`.
   Expected: log lines "Private key migration: N migrated, M already encrypted, 0 failed" and the backup warning; `enc_prv_key` values in MongoDB now start with `-----BEGIN ENCRYPTED PRIVATE KEY-----`; issuing a certificate from an existing CA works; restarting logs 0 migrated.

## Test Coverage

- Tests added: 9
- Coverage: not measured
- Status: passing (352/352)

## Ready for Review

- [x] All acceptance criteria met
- [x] Tests passing
- [x] No critical issues
- [ ] Documentation updated (`key-encryption-docs`)
- [x] Developer notes captured

## Developer Notes

Use the same `NOTARY_APP_KEY` that older builds ran with: legacy 3DES rows can only be upgraded with it. Plaintext rows never used it, so any valid value works for them, but from this point on that value is the only way to read the keys. The docs item should say all of this, and that exposure was limited to keys created from `f9aadd0` onward.

---
*Generated by specs.md - fabriqa.ai FIRE Flow Run run-specs-md-fire-planner-fad6e4-004*
