---
id: pkcs12-modern-export-with-chain
title: Modern PKCS
intent: modern-pkcs12-export
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-25T00:00:00Z
run_id: run-github-issue-51-4dca63-004
completed_at: 2026-10-25T01:26:18.695Z
---

# Work Item: Modern PKCS #12 export with the issuing chain

## Description

Build the PKCS #12 export with the .NET Pkcs12Builder (AES-256 protection, a SHA-256 MAC, a high iteration count) and include every issuing certificate up to the root.

## Acceptance Criteria

- [ ] Key bag and certificate bags are protected with PBES2: AES-256-CBC with PBKDF2-HMAC-SHA256; the MAC is HMAC-SHA256; the iteration count is at least 600,000
- [ ] The bundle holds the leaf with its private key and each issuing certificate, found by following `IssuingSlug`, up to the root; a self-signed certificate exports alone
- [ ] A broken chain (missing issuer record) fails with a clear error rather than exporting a partial chain silently
- [ ] The leaf key entry has a friendly name and local key id so tools pair key and certificate
- [ ] The password is required as today; the private key is never logged

## Technical Notes

`src/library/service/CertificateService.cs` `RequestCertificateAsync` (Pkcs12 case, ~line 220). Walk `Certificate.IssuingSlug` with `GetAsync`, guarding against loops and a sensible depth limit. BouncyCastle cannot set the MAC digest or iteration counts, so convert the BouncyCastle private key to PKCS #8 and use `System.Security.Cryptography.Pkcs12Builder` (`AddShroudedKey` / encrypted safe contents with `PbeParameters`, `SealWithMac`). Put the PKCS #12 construction in a small helper class so it can be unit-tested without the repository. `src/web/Shared/DownloadCertificateDialog.razor.cs` needs no change for this item.

## Dependencies

(none)
