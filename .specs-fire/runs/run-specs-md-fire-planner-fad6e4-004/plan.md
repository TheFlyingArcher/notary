---
run: run-specs-md-fire-planner-fad6e4-004
work_item: plaintext-key-migration
intent: encrypt-stored-private-keys
mode: validate
checkpoint: plan
approved_at: 2026-10-16T00:00:00Z
---

# Implementation Plan: Re-encrypt existing keys at startup

## Approach

Add `MigrateStoredKeysAsync()` to `IAsymmetricKeyService`/`AsymmetricKeyService`, called once from `src/web/Program.cs` after `builder.Build()` and before `app.Run()`. It reads every `AsymmetricKey` row (active or not) and brings each into the PBES2 format written by `WriteEncryptedKey`. No key material changes, so certificates stay valid.

### Finding that changes the scope

Git history shows keys were not always plaintext. Before commit `f9aadd0` ("Enhancing key security") the code used `new Pkcs8Generator(key, Pkcs8Generator.PbeSha1_3DES)`, which really did encrypt (PKCS#8, SHA-1 + 3DES, the default low iteration count). So a database can hold three kinds of rows:

| Stored form | Origin | Migration action |
| --- | --- | --- |
| `ENCRYPTED PRIVATE KEY`, PBES2 / PBKDF2-HMAC-SHA256 / AES-256-CBC | New code | Skip (counted as already encrypted) |
| `ENCRYPTED PRIVATE KEY`, PBES1 3DES | Before `f9aadd0` | Decrypt with `ApplicationKey`, re-encrypt as PBES2 (weak, so upgraded too) |
| `PRIVATE KEY` (plaintext PKCS #8) | `f9aadd0` onward | Re-encrypt as PBES2 |

Anything else (garbage, another PEM type, wrong passphrase on a legacy row) is a failure for that row only. This also means the docs can say precisely which versions exposed keys in plaintext (those from `f9aadd0` on), instead of "all earlier versions".

### Key decisions

| Decision | Choice | Rationale |
| --- | --- | --- |
| Reading old formats | BouncyCastle `OpenSsl.PemReader` with a small password finder, the same call the old loader used | Handles both plaintext and PBES1 3DES with proven code; the strict reader in `ReadEncryptedKey` stays strict |
| Safety check before overwrite | Encrypt, then decrypt the new value with `ReadEncryptedKey` and require the key to equal the original; only then save | An unreadable row can never replace a readable one. Cost is two PBKDF2 runs per key (about 0.6 s here, a few seconds on a Pi, one time) |
| Write | `Repository.SaveAsync(entity)` with the new bytes, `Updated` and `UpdatedBySlug` set | Replace-by-slug upsert; does not go through `AsymmetricKeyService.SaveAsync`, so no new key is generated |
| Failures | Per-row failures are logged with the key slug only, counted, and do not stop the loop or startup; a failure to read the key list propagates and stops startup | A bad row must not take down a CA; an unreadable database should be loud |
| Idempotence | Already-PBES2 rows are recognised by parsing the PEM and OIDs (no decryption), so a second run changes nothing | Cheap and safe to run on every start |
| Concurrency | Two instances starting together may both rewrite a row; both results are valid and the last wins | No locking needed |
| Result | `KeyMigrationResult(Migrated, AlreadyEncrypted, Failed)` record in `Notary.Contract`, summary logged at Info; a Warn when anything was migrated saying older database backups still hold unprotected keys | Operators see what happened |
| Loader after migration | Already done in run 002: `LoadKeyPair` refuses plaintext | Test added here against a migration-failed row |
| Backups | The migration does not write plaintext anywhere | Per the work item |

## Files to Create

| File | Purpose |
|------|---------|
| `src/library/core/Contract/KeyMigrationResult.cs` | Positional record with the three counts |

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/core/Interface/Service/IAsymmetricKeyService.cs` | Add `Task<KeyMigrationResult> MigrateStoredKeysAsync()` |
| `src/library/service/AsymmetricKeyService.cs` | Implement migration, format classification, legacy reader |
| `src/web/Program.cs` | After `Build()`, create a scope and run the migration |
| `src/test/InMemoryFakes.cs` | `FakeKeyService` gets the new interface member |
| `src/test/AsymmetricKeyServiceTest.cs` | Migration tests |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/AsymmetricKeyServiceTest.cs` | Plaintext RSA and EC rows migrated and still load, sign and keep the same public key; legacy 3DES row (built with `Pkcs8Generator(key, PbeSha1_3DES)`) migrated; PBES2 row untouched byte for byte; second run is a no-op; garbage row and legacy-wrong-passphrase row counted as failed and left unchanged while other rows still migrate; inactive rows migrated; counts correct; missing application key throws |

## Technical Details

Verification: `dotnet build`, `dotnet test ./src/test/`. The startup wiring has no automated test (same as the existing config call); it is checked in Visual Studio UAT against a database containing plaintext keys, and the walkthrough will say how. The web project is built to prove the wiring compiles.

---
