---
id: run-github-issue-51-4dca63-004
scope: batch
work_items:
  - id: pkcs12-modern-export-with-chain
    intent: modern-pkcs12-export
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
  - id: pkcs7-chain-export
    intent: modern-pkcs12-export
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
  - id: certificate-export-tests
    intent: modern-pkcs12-export
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
current_item: null
status: completed
started: 2026-10-25T01:20:48.993Z
completed: 2026-10-25T01:26:19.369Z
---

# Run: run-github-issue-51-4dca63-004

## Scope
batch (3 work items)

## Work Items
1. **pkcs12-modern-export-with-chain** (confirm) — completed
2. **pkcs7-chain-export** (confirm) — completed
3. **certificate-export-tests** (confirm) — completed


## Current Item
(all completed)

## Files Created
- `src/library/service/CertificateChainResolver.cs`: Resolve a certificate and its issuing chain
- `src/library/service/Pkcs12ExportBuilder.cs`: Build modern PKCS #12 (AES-256, SHA-256 MAC, 600k iterations)
- `src/test/CertificateExportTest.cs`: Round-trip and OpenSSL tests for PKCS #12 and PKCS #7

## Files Modified
- `src/library/service/CertificateService.cs`: Pkcs12 uses the builder with chain; Pkcs7 certs-only export
- `src/library/service/Notary.Service.csproj`: Reference patched System.Security.Cryptography.Pkcs 10.0.13
- `src/web/Shared/DownloadCertificateDialog.razor`: PKCS #7 option
- `src/web/Shared/DownloadCertificateDialog.razor.cs`: .p7b file name

## Decisions
(none)


## Summary

- Work items completed: 3
- Files created: 3
- Files modified: 4
- Tests added: 19
- Coverage: 73%
- Completed: 2026-10-25T01:26:19.369Z
