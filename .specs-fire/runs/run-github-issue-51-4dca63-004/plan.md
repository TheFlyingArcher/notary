---
run: run-github-issue-51-4dca63-004
work_item: pkcs12-modern-export-with-chain, pkcs7-chain-export, certificate-export-tests
intent: modern-pkcs12-export
mode: confirm
checkpoint: plan
approved_at: 2026-10-25T01:30:00Z
---

# Implementation Plan: modern PKCS #12 / PKCS #7 export

BouncyCastle cannot set the PKCS #12 MAC digest or iteration counts (checked in 2.2.1 and 2.7.0), so PKCS #12 is built with .NET `Pkcs12Builder`. The BouncyCastle package migration was dropped at the user's request (no longer needed).

## Approach

1. `CertificateChainResolver` follows `IssuingSlug` from the certificate to the root (loop guard, depth limit, clear error for a missing issuer) and returns parsed certificates leaf first.
2. `Pkcs12ExportBuilder`: BouncyCastle private key to PKCS #8, imported into `ECDsa`/`RSA`; shrouded key bag with PBES2 AES-256-CBC, PBKDF2-HMAC-SHA256, 600,000 iterations; leaf and chain in encrypted safe contents with the same parameters; friendly name and local key id on the leaf; `SealWithMac` SHA-256 at 600,000 iterations.
3. `RequestCertificateAsync`: Pkcs12 uses the builder; new Pkcs7 case returns a certs-only `.p7b` (BouncyCastle `CmsSignedDataGenerator`, no signers) with leaf plus chain.
4. Download dialog: PKCS #7 option, `.p7b` file name, password field only for PKCS #12.
5. Tests: `CertificateExportTest` round trips for RSA and EC, ASN.1 assertions of algorithms and iteration count, failures, and an OpenSSL test (no `-legacy`) skipped when `openssl` is absent.

## Files to Create

| File | Purpose |
|------|---------|
| `src/library/service/CertificateChainResolver.cs` | Resolve a certificate and its issuing chain |
| `src/library/service/Pkcs12ExportBuilder.cs` | Build the modern PKCS #12 |
| `src/test/CertificateExportTest.cs` | Round-trip and OpenSSL tests |

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/service/CertificateService.cs` | Pkcs12 and Pkcs7 cases |
| `src/web/Shared/DownloadCertificateDialog.razor(.cs)` | PKCS #7 option, `.p7b`, password only for PKCS #12 |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/CertificateExportTest.cs` | PKCS #12 and PKCS #7 round trips, algorithms, chain, failures, OpenSSL check |

## Technical Details

UAT: open a downloaded `.pfx` with `openssl pkcs12 -info` without `-legacy`; import on Windows and macOS. Existing downloads are unchanged.

---
*Plan approved at checkpoint. Execution follows.*
