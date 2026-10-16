---
id: encrypt-stored-private-keys
title: Encrypt stored private keys
status: completed
created: 2026-10-15T00:00:00Z
source: security finding from the EC implementation review (follow-up to issue
completed_at: 2026-10-16T00:23:18.158Z
---

# Intent: Encrypt stored private keys

## Goal

Make stored CA and certificate private keys actually encrypted at rest: PKCS#8 `ENCRYPTED PRIVATE KEY` using PBES2 with PBKDF2-HMAC-SHA256 and AES-256-CBC (RFC 8018, RFC 5958, NIST SP 800-132), protected by the `NOTARY_APP_KEY` passphrase.

## Users

Notary operators who rely on the database (and its backups) not exposing private keys.

## Problem

`AsymmetricKeyService.WriteKeyWithPbes2Aes256` builds `new Pkcs8Generator(key) { Password, SecureRandom, IterationCount }` with no algorithm. In BouncyCastle 2.2.1 that ignores the password and writes a plaintext `PRIVATE KEY` PEM, for RSA and EC keys alike. `PemReader` loads it back without a password, so the problem is invisible at runtime. Separately, `WriteKey` writes plaintext whenever the passphrase is null, and nothing validates `NOTARY_APP_KEY`. Anyone who can read the database or a backup can read every CA private key.

## Success Criteria

- Newly stored keys (RSA, P-256, P-384, P-521) start with `-----BEGIN ENCRYPTED PRIVATE KEY-----` and use PBES2 / PBKDF2-HMAC-SHA256 / AES-256-CBC
- Loading with a wrong passphrase throws; no code path writes a plaintext key
- Startup fails if `NOTARY_APP_KEY` is missing or shorter than 16 characters
- Existing plaintext rows are re-encrypted at startup; already-encrypted rows are skipped; the migration is idempotent and logs counts
- After migration the loader refuses plaintext keys
- The exposure, the fix and a recommendation to rotate CA keys are documented in the public repo
- Tests cover the above; existing tests pass

## Constraints

- BouncyCastle.NetCore 2.2.1: `Pkcs8Generator` only names legacy PBE algorithms, so hand-build `EncryptedPrivateKeyInfo` (upgrade BouncyCastle only if that proves impossible)
- Fail closed: no catch-and-retry fallback to weaker or no encryption
- The passphrase stays `NOTARY_APP_KEY`, shared by all keys (PBKDF2 gives each key its own salt)
- Windows dev environment, no Docker; verify with `dotnet build` and `dotnet test`
- Out of scope: a re-key command for changing `NOTARY_APP_KEY`, per-key passphrases, KMS/HSM, PKCS#12 export algorithms

## Notes

The user approved documenting this openly in the public repository because it is being fixed now. Changing `NOTARY_APP_KEY` after this work orphans every stored key; the docs must say so.
