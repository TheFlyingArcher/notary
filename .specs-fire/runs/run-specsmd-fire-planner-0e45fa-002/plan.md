---
run: run-specsmd-fire-planner-0e45fa-002
work_item: private-key-encryption-guard
intent: download-dialog-friendly-ux
mode: autopilot
checkpoint: none
approved_at: n/a
---

# Implementation Plan: Lock in "private key is always encrypted"

## Approach

Inspection shows the rule already holds: `ExportPkcs12Async` and `Pkcs12ExportBuilder.Build` both throw on a blank password, the key goes out as a shrouded (PBES2 AES-256) key bag, and `Pkcs12_WithoutAPassword_IsRefused` and `Pkcs12_WrongPassword_CannotOpenTheFile` already pin both. No production change is needed.

What is not yet pinned is the other half: no format puts the private key in the clear. Add tests that, for every format, the download does not contain the key's PKCS #8 bytes or a `PRIVATE KEY` PEM label, and that a PKCS #12 only contains it encrypted. Add a short note to `docs/private-key-storage.md`.

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `src/test/CertificateExportTest.cs` | Tests that no export contains the plaintext private key |
| `docs/private-key-storage.md` | Note on how private keys leave Notary on download |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/CertificateExportTest.cs` | Der, Pem, Pkcs7, Pkcs7Pem, and Pkcs12 (EC and RSA): output has no plaintext PKCS #8 key bytes and no `PRIVATE KEY` label |

---
*Autopilot: no checkpoint.*
