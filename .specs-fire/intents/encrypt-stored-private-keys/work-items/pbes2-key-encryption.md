---
id: pbes2-key-encryption
title: PBES2 AES-256 private key encryption
intent: encrypt-stored-private-keys
complexity: high
mode: validate
status: completed
depends_on: []
created: 2026-10-15T00:00:00Z
run_id: run-specs-md-fire-planner-fad6e4-002
completed_at: 2026-10-16T00:07:44.491Z
---

# Work Item: PBES2 AES-256 private key encryption

## Description

Replace the broken `Pkcs8Generator` password path with a real PKCS#8 `EncryptedPrivateKeyInfo` writer and reader (PBES2, PBKDF2-HMAC-SHA256, AES-256-CBC) in `AsymmetricKeyService`, failing closed.

## Acceptance Criteria

- [ ] Stored keys for RSA, P-256, P-384 and P-521 begin with `-----BEGIN ENCRYPTED PRIVATE KEY-----`
- [ ] The ASN.1 shows PBES2 with PBKDF2 (HMAC-SHA256, at least 100,000 iterations, random salt of 16+ bytes) and AES-256-CBC with a random IV
- [ ] Round trip works for RSA and each EC curve: the loaded key signs and the original public key verifies
- [ ] Loading with a wrong passphrase throws
- [ ] A null or empty passphrase makes the writer throw; no code path writes a plaintext key (remove the unencrypted branch and the catch-and-retry fallback)
- [ ] The writer throws if its output is not `ENCRYPTED PRIVATE KEY`
- [ ] Code comments describe what actually runs
- [ ] `openssl pkey -in key.pem -passin ...` can read a produced key if OpenSSL is available (manual check, optional)

## Technical Notes

`src/library/service/AsymmetricKeyService.cs`. BC 2.2.1 `Pkcs8Generator` rejects AES algorithm names; build `PBES2Parameters`/`Pbkdf2Params` and `EncryptedPrivateKeyInfo` by hand (a scratch probe showed `EncryptedPrivateKeyInfoFactory` does not take the PBES2 arguments directly). Prefer `PemObject("ENCRYPTED PRIVATE KEY", ...)` written with `PemWriter`; read with `PemReader` plus a custom decrypt of `EncryptedPrivateKeyInfo` if the stock reader does not handle PBES2 on 2.2.1. Keep `PasswordFinder` or replace it. The loader must still accept plaintext only through the migration item's explicit path. Tests go in a new `AsymmetricKeyServiceTest.cs` and generate keys in-test.

## Dependencies

(none)
