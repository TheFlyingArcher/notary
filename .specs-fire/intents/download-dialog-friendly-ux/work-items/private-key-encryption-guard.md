---
id: private-key-encryption-guard
title: Lock in "private key is always encrypted"
intent: download-dialog-friendly-ux
complexity: low
mode: autopilot
status: completed
depends_on: []
created: 2026-10-28T00:00:00Z
run_id: run-specsmd-fire-planner-0e45fa-002
completed_at: 2026-10-28T06:02:29.362Z
---

# Work Item: Lock in "private key is always encrypted"

## Description

PKCS #12 is the only export that contains a private key. Pin with a test that it is never produced without a password, and document that the key leaves Notary only inside the password-protected bundle.

## Acceptance Criteria

- [ ] A test shows `RequestCertificateAsync` with `CertificateFormat.Pkcs12` and a blank password does not return an unencrypted key (it fails or is rejected)
- [ ] A test shows the PKCS #12 output cannot be opened without the password and opens with it
- [ ] If the service accepts a blank password today, it is changed to reject it
- [ ] A short docs note states that private keys are only exported in the password-protected PKCS #12 bundle and that other formats never contain a key
- [ ] Solution builds cleanly and existing tests pass

## Technical Notes

`src/library/service/CertificateService.cs` (`RequestCertificateAsync`, `ExportPkcs12Async`) and `src/library/service/Pkcs12ExportBuilder.cs`. Extend `src/test/CertificateExportTest.cs`. Search `docs/` and `README.md` for the existing PKCS #12 export text.

## Dependencies

(none)
