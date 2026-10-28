---
id: pkcs7-dialog-options
title: PKCS
intent: pkcs7-pem-export
complexity: low
mode: autopilot
status: completed
depends_on:
  - pkcs7-pem-export
created: 2026-10-27T00:00:00Z
run_id: run-pkcs7-bundle-bug-rca-8da318-002
completed_at: 2026-10-28T00:52:54.437Z
---

# Work Item: PKCS #7 PEM and DER options in the download dialog

## Description

Offer the two PKCS #7 encodings as separate radio options in the download dialog, name both downloads `.p7b`, and update any docs that mention PKCS #7.

## Acceptance Criteria

- [ ] The dialog shows "PKCS #7 (PEM, chain)" for `Pkcs7Pem` and "PKCS #7 (DER, chain)" for `Pkcs7`
- [ ] Both options download as `<name>.p7b`
- [ ] The password fields stay disabled for both PKCS #7 options (only PKCS #12 enables them)
- [ ] The dialog works from `/ca`, `/certificates` and the certificate detail page (all use the same dialog)
- [ ] Docs and README mentions of PKCS #7 are updated to describe both encodings and the `-inform DER` hint for the DER file
- [ ] Solution builds cleanly and existing tests pass

## Technical Notes

`src/web/Shared/DownloadCertificateDialog.razor` and `.razor.cs` (the filename `switch` needs a `Pkcs7Pem` case). No change to `DownloadCertificateViewModel` expected. Search `docs/`, `README.md` and `deploy/` for "PKCS". UAT: download each encoding from `/ca`; the PEM file opens with plain `openssl pkcs7 -in f.p7b -noout`, the DER file needs `-inform DER`.

## Dependencies

- pkcs7-pem-export
