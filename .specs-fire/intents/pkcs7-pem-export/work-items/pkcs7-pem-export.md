---
id: pkcs7-pem-export
title: PEM-encoded PKCS
intent: pkcs7-pem-export
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-27T00:00:00Z
run_id: run-pkcs7-bundle-bug-rca-8da318-001
completed_at: 2026-10-28T00:50:18.611Z
---

# Work Item: PEM-encoded PKCS #7 export

## Description

Add `CertificateFormat.Pkcs7Pem` and have `RequestCertificateAsync` return the certs-only PKCS #7 chain as PEM (`-----BEGIN PKCS7-----`), so `openssl pkcs7 -in bundle.p7b` works without `-inform DER`. The existing DER `Pkcs7` is unchanged.

## Acceptance Criteria

- [ ] `CertificateFormat.Pkcs7Pem = 16` is added; `Der`, `Pkcs7`, `Pkcs12` and `Pem` keep their values
- [ ] `RequestCertificateAsync` with `Pkcs7Pem` returns PEM text with the `PKCS7` label whose decoded body equals the DER export of the same certificate
- [ ] The chain is gathered by the existing `GetChainAsync`; no second PKCS #7 builder
- [ ] A missing certificate still returns null; an unknown format still throws `ArgumentException`
- [ ] Tests: PEM output starts with `-----BEGIN PKCS7-----`, parses with `CmsSignedData` to the leaf and every issuer (RSA and EC), has no signers or keys, and a self-signed certificate yields one certificate
- [ ] OpenSSL test (skipped when `openssl` is unavailable): `openssl pkcs7 -in <file> -noout` exits 0 with no `-inform`, and `-print_certs` lists three subjects for a leaf + intermediate + root chain
- [ ] The existing DER tests, including `OpenSsl_ReadsThePkcs7Chain`, still pass

## Technical Notes

`src/library/core/CertificateFormat.cs`, `src/library/service/CertificateService.cs` (`RequestCertificateAsync`, `ExportPkcs7Async`), `src/test/CertificateExportTest.cs`. Wrap the DER bytes with BouncyCastle's `PemWriter` / `PemObject("PKCS7", der)`, or Base64 in 64-character lines with the PEM header and footer. Use LF line endings or match the existing PEM export. Keep the method that produces DER and add a thin PEM wrapper around it.

## Dependencies

(none)
