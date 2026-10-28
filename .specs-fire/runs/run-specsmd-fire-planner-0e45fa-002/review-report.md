---
run: run-specsmd-fire-planner-0e45fa-002
work_item: private-key-encryption-guard
intent: download-dialog-friendly-ux
generated: 2026-10-28T02:02:00Z
---

# Code Review: Lock in "private key is always encrypted"

Files reviewed: `src/test/CertificateExportTest.cs`, `docs/private-key-storage.md`

## Auto-fixed

None needed.

## Checked, no change needed

- The new test follows the file's style (Allman, NUnit 4, `[Values]` for the matrix) and generates keys in-test; no key material is committed.
- The search is by raw bytes of both the PKCS #8 encoding and the private value, plus the `PRIVATE KEY` PEM label, so a key emitted in DER or PEM form would be caught.
- Docs wording matches the code: AES-256 with PBKDF2-HMAC-SHA256, password not stored, blank password refused.

## Suggestions (not applied)

None. No security or architecture issues found.
