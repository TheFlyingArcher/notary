---
id: pkcs7-chain-export
title: PKCS
intent: modern-pkcs12-export
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-25T00:00:00Z
run_id: run-github-issue-51-4dca63-004
completed_at: 2026-10-25T01:26:19.029Z
---

# Work Item: PKCS #7 certificate chain export

## Description

Implement `CertificateFormat.Pkcs7` as a certs-only PKCS #7 (`.p7b`) containing the certificate and its issuing chain, and offer it in the download dialog.

## Acceptance Criteria

- [ ] `RequestCertificateAsync` with `Pkcs7` returns DER bytes of a certs-only SignedData with the leaf and every issuing certificate; no private key, no password needed
- [ ] The download dialog offers PKCS #7, names the file `<name>.p7b`, and does not ask for a password for it
- [ ] The chain is gathered with the same helper as the PKCS #12 export
- [ ] The `TODO` is removed and no format returns null for a found certificate

## Technical Notes

Use BouncyCastle `CmsSignedDataGenerator` with a certificate store and no signers (certs-only). Reuse the chain-walking helper from `pkcs12-modern-export-with-chain` (create it in whichever item runs first, in its own small class). `src/web/Shared/DownloadCertificateDialog.razor(.cs)` and `DownloadCertificateViewModel`.

## Dependencies

(none)
