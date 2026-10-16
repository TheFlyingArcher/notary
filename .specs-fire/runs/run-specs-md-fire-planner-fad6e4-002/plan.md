---
run: run-specs-md-fire-planner-fad6e4-002
work_item: pbes2-key-encryption
intent: encrypt-stored-private-keys
mode: validate
checkpoint: plan
approved_at: 2026-10-15T00:00:00Z
---

# Implementation Plan: PBES2 AES-256 private key encryption

## Approach

Replace the `Pkcs8Generator` password path in `AsymmetricKeyService` with a hand-built PKCS#8 `EncryptedPrivateKeyInfo` (RFC 8018 PBES2 / RFC 5958), and add a matching reader. A scratch probe on BouncyCastle.NetCore 2.2.1 confirmed the approach: the output is `ENCRYPTED PRIVATE KEY`, round-trips, a wrong password fails, and OpenSSL 3.5 (`openssl pkey`) decrypts it.

### Key decisions

| Decision | Choice | Rationale |
| --- | --- | --- |
| Construction | `Pkcs5S2ParametersGenerator(Sha256Digest)` derives a 32-byte key; `PaddedBufferedBlockCipher(CbcBlockCipher(AesEngine))` encrypts the `PrivateKeyInfo` DER; wrapped in `EncryptedPrivateKeyInfo` with `PbeS2Parameters(Pbkdf2Params(salt, iter, 32, hmacWithSHA256), aes256-cbc IV)` | Works on 2.2.1 without a BouncyCastle upgrade; standard OIDs, so any PKCS#8 reader can open it |
| Iterations | 600,000 (OWASP 2023 figure for PBKDF2-HMAC-SHA256); work item minimum was 100,000 | Exceeds NIST SP 800-132 minimums. The cost is paid when a key is loaded (each issuance/CRL); to be measured, and a constant makes it easy to lower |
| Salt / IV | 16 random bytes each from `SecureRandom`, fresh per key | Standard |
| Passphrase | UTF-8 bytes of `ApplicationKey` | Same secret as today; empty/null throws |
| Reader | Parse PEM with the generic `Utilities.IO.Pem` reader, require type `ENCRYPTED PRIVATE KEY` and the PBES2 / PBKDF2-HMAC-SHA256 / AES-256-CBC OIDs, derive and decrypt using the stored salt, iterations and IV, then `PrivateKeyFactory.CreateKey` | Reads only what we write; unknown algorithms are rejected |
| Reader safety | Reject iteration counts above 10,000,000 or salt under 8 bytes; any padding or parse failure becomes one `InvalidOperationException("The private key could not be decrypted ...")` that never includes key material | A tampered row cannot make startup spin; wrong password and corruption look the same |
| Plaintext PEM on load | `LoadKeyPair` throws for a plaintext `PRIVATE KEY`; a separate internal method `LoadPlaintextForMigration` is added for the migration item to use | Fail closed, but keep the data recoverable for `plaintext-key-migration` |
| Writer | Single path `WriteEncryptedKey(key, password, random)`; remove the unencrypted branch, the `Pkcs8Generator` use and the catch-and-retry fallback; assert the output starts with `-----BEGIN ENCRYPTED PRIVATE KEY-----` | The `GetPublicKey` use of `WriteKey(keyPair.Public)` is already broken (issue #47); it is changed to return the SubjectPublicKeyInfo DER, which removes the last unencrypted writer |
| Out of scope here | Config validation (next item), migration (later item), #43 named curves | Separate work items / issues |

### Wrong-password behavior

CBC padding check fails for about 255 of 256 wrong passwords; the remaining case yields garbage that fails the PKCS#8 parse. Both are mapped to the same exception.

## Files to Create

| File | Purpose |
| --- | --- |
| `src/test/AsymmetricKeyServiceTest.cs` | Tests for the writer and reader |

## Files to Modify

| File | Changes |
| --- | --- |
| `src/library/service/AsymmetricKeyService.cs` | New PBES2 writer and reader; `LoadKeyPair` requires encrypted keys; delete `WriteKeyWithPbes2Aes256`, the unencrypted `WriteKey` branch and fallback; fix `GetPublicKey`; add migration-only plaintext loader (internal) |
| `src/library/service/PasswordFinder.cs` | Removed if nothing else uses it |
| `src/test/*` (existing) | Any existing test that builds a plaintext key fixture is updated |

## Tests

| Test File | Coverage |
| --- | --- |
| `src/test/AsymmetricKeyServiceTest.cs` | Stored bytes start with `-----BEGIN ENCRYPTED PRIVATE KEY-----` for RSA, P-256, P-384, P-521; ASN.1 has PBES2, PBKDF2, hmacWithSHA256, AES-256-CBC, 600,000 iterations, 16-byte salt and IV; round trip signs and verifies; wrong password throws; corrupted ciphertext throws; null/empty passphrase throws; two writes of the same key differ (random salt/IV); plaintext PEM is rejected by the loader; absurd iteration count rejected; `GetPublicKey` returns a parseable SubjectPublicKeyInfo |

## Technical Details

Verification: `dotnet build`, `dotnet test ./src/test/`, plus a manual `openssl pkey -in key.pem -passin pass:...` check on a generated key using a throwaway passphrase. Timing of one load at 600,000 iterations is measured and recorded in the test report; if it is too slow for a Raspberry Pi the constant is revisited before merge.

## Based on Design Doc

(none; decisions captured above)

---
