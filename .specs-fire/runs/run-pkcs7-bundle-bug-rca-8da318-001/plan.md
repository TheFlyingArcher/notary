---
run: run-pkcs7-bundle-bug-rca-8da318-001
work_item: pkcs7-pem-export
intent: pkcs7-pem-export
mode: confirm
checkpoint: plan
approved_at: 2026-10-27T23:30:00Z
---

# Implementation Plan: PEM-encoded PKCS #7 export

## Approach

Add `CertificateFormat.Pkcs7Pem = 16`. In `CertificateService.RequestCertificateAsync` add a `Pkcs7Pem` case that calls a new thin `ExportPkcs7PemAsync`, which takes the DER bytes from the existing `ExportPkcs7Async` and wraps them with `System.Security.Cryptography.PemEncoding.WriteString("PKCS7", der)` (RFC 7468: 64-column Base64, LF line breaks), followed by a trailing newline, as ASCII bytes. No second PKCS #7 builder; chain gathering stays in `GetChainAsync`. `Pkcs7 = 2` (DER) is untouched.

## Files to Create

(none)

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/core/CertificateFormat.cs` | Add `Pkcs7Pem = 16` |
| `src/library/service/CertificateService.cs` | New `Pkcs7Pem` switch case and `ExportPkcs7PemAsync` wrapper over the DER export |
| `src/test/CertificateExportTest.cs` | New PEM tests (below) |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/CertificateExportTest.cs` | `Pkcs7Pem_IsPemArmoredAndDecodesToTheDerExport` (RSA and EC): starts with `-----BEGIN PKCS7-----`, ends with the footer, body decodes to the same bytes as the `Pkcs7` export and parses with `CmsSignedData` to leaf + intermediate + root with no signers. `Pkcs7Pem_OfASelfSignedCertificate_ContainsOnlyThatCertificate`. `OpenSsl_ReadsThePkcs7PemChainWithoutInform`: `openssl pkcs7 -in f -noout` exits 0 and `-print_certs` lists 3 subjects (skipped if openssl is absent). Existing DER tests unchanged. |

## Technical Details

Missing certificate still returns null (handled before the switch); unknown format still throws `ArgumentException`. Requires no password. Decision: `PemEncoding` from the BCL rather than BouncyCastle `PemWriter`, to get RFC 7468 line wrapping and LF endings without a stream writer.

---
*Plan approved at checkpoint. Execution follows.*
