---
id: download-dialog-friendly-ux
title: Friendlier certificate download dialog
status: completed
created: 2026-10-28T00:00:00Z
completed_at: 2026-10-28T06:07:31.410Z
---

# Intent: Friendlier certificate download dialog

## Goal

Make the certificate download dialog understandable without cryptography knowledge: friendly format names, an explanatory blurb, password fields shown only when relevant, and an optional file name. Source: GitHub issue #82.

## Users

Anyone using the Notary web UI to download a certificate.

## Problem

The radios are jargon (DER, PEM, PKCS #12, PKCS #7), so nobody learns that "PKCS #12" includes the private key. The password fields are disabled rather than hidden when they do not apply, and the download file name is fixed to the certificate's `Name`.

## Success Criteria

- Radios use friendly names:
  - Certificate only (.cer)
  - Certificate only, text (.pem)
  - Certificate + private key bundle (.pfx)
  - Certificate chain, text (.p7b)
  - Certificate chain, binary (.p7b)
- A blurb at the bottom changes with the selected format: what it contains and common uses, and it states plainly that the bundle includes the private key
- Password and Confirm Password fields are shown only for the private key bundle (PKCS #12)
- An optional file name box defaults to blank, which uses the certificate's `Name`; the app appends the extension for the chosen format and strips or rejects characters invalid in file names
- A private key is never exported unencrypted (PKCS #12 already requires a password; lock this in with a test)

## Constraints

- Web UI only: `src/web/Shared/DownloadCertificateDialog.razor` and its code-behind; `CertificateFormat` and the export services are unchanged
- Existing password rules stay (8+ characters, upper, lower, digit)
- Allman braces, NUnit 4 + Moq; changes must build and run from the Visual Studio solution for UAT
- Out of scope: a standalone encrypted PEM private key export (possible later iteration; the issue says this may be a series of iterations)

## Notes

Decisions confirmed with the user: private-key encryption requirement is already satisfied (test only), suggested friendly names, extension auto-appended with sanitizing, dynamic blurb for the selected format only.
