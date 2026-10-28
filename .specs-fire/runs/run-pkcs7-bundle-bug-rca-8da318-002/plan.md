---
run: run-pkcs7-bundle-bug-rca-8da318-002
work_item: pkcs7-dialog-options
intent: pkcs7-pem-export
mode: autopilot
---

# Implementation Plan: PKCS #7 PEM and DER options in the download dialog

## Approach
Two radio options in the dialog, shared `.p7b` filename case, short doc note in the bootstrap runbook.

## Files to Modify
| File | Changes |
|------|---------|
| `src/web/Shared/DownloadCertificateDialog.razor` | "PKCS #7 (PEM, chain)" and "PKCS #7 (DER, chain)" radios |
| `src/web/Shared/DownloadCertificateDialog.razor.cs` | `Pkcs7Pem` shares the `.p7b` filename case |
| `docs/initial-ca-bootstrap.md` | Note on both encodings and `-inform DER` |

## Tests
No new unit test: Blazor markup and a filename switch. Covered by the build, the existing suite, and manual UAT.
