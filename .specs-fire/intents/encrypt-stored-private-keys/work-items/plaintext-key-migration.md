---
id: plaintext-key-migration
title: Re-encrypt existing plaintext keys at startup
intent: encrypt-stored-private-keys
complexity: high
mode: validate
status: completed
depends_on:
  - pbes2-key-encryption
  - app-key-validation
created: 2026-10-15T00:00:00Z
run_id: run-specs-md-fire-planner-fad6e4-004
completed_at: 2026-10-16T00:21:18.470Z
---

# Work Item: Re-encrypt existing plaintext keys at startup

## Description

Rows already stored as plaintext `PRIVATE KEY` must be encrypted in place, once, without breaking certificates that reference them. Afterwards the loader refuses plaintext.

## Acceptance Criteria

- [ ] On startup, after configuration validation, every `AsymmetricKey` row whose `EncryptedPrivateKey` is a plaintext `PRIVATE KEY` PEM is re-encrypted and saved; the key material (and therefore certificates) is unchanged
- [ ] Rows already `ENCRYPTED PRIVATE KEY` are skipped; running twice changes nothing
- [ ] One bad row does not abort the rest; failures are logged with the key slug (never key material) and startup continues, with a clear summary
- [ ] The summary logs counts: migrated, already encrypted, failed
- [ ] After migration `LoadKeyPair` throws for a plaintext key instead of loading it
- [ ] Tests with mixed rows: plaintext, encrypted, corrupt; idempotence; key still signs after migration

## Technical Notes

Add a method such as `MigratePlaintextKeysAsync` to `IAsymmetricKeyService`/`AsymmetricKeyService` using `IAsymmetricKeyRepository.GetAllAsync` and `SaveAsync` (the repository base, not the service `SaveAsync` that generates keys when the stored key is null). Call it from `src/web/Program.cs` after `builder.Build()` and before `app.Run()`. Detect plaintext by PEM header, not by trying passwords. Back up advice goes in the docs item; do not write backups of plaintext keys to disk.

## Dependencies

- pbes2-key-encryption
- app-key-validation
