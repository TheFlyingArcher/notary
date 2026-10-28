---
id: download-format-catalog
title: Download format catalog and file name builder
intent: download-dialog-friendly-ux
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-28T00:00:00Z
run_id: run-specsmd-fire-planner-0e45fa-001
completed_at: 2026-10-28T01:26:52.564Z
---

# Work Item: Download format catalog and file name builder

## Description

Add a plain, testable class holding the display metadata for each `CertificateFormat`, plus a builder for the download file name. The dialog (next item) consumes it, so none of this logic lives in Razor markup.

## Acceptance Criteria

- [ ] For every `CertificateFormat` value the catalog gives a friendly label, file extension, blurb (what it contains and common uses) and whether a password is required
- [ ] Labels: Certificate only (.cer); Certificate only, text (.pem); Certificate + private key bundle (.pfx); Certificate chain, text (.p7b); Certificate chain, binary (.p7b)
- [ ] The private key bundle blurb states that it includes the private key and is password protected; no other blurb implies a key
- [ ] The file name builder returns the certificate `Name` plus extension when the input is blank or whitespace
- [ ] A user-supplied base name gets the extension for the chosen format; a trailing copy of that extension is not doubled
- [ ] Characters invalid in file names (`Path.GetInvalidFileNameChars()`, and path separators) are stripped; a name that is empty after stripping falls back to the certificate `Name`
- [ ] NUnit tests cover every format, blank input, sanitizing, and extension handling
- [ ] Solution builds cleanly and existing tests pass

## Technical Notes

Put the class in `src/web` (for example `src/web/ViewModels` or a new folder alongside it), registered nowhere unless it needs state; static or a small injected service is fine, following Allman braces. Tests go in `src/test`. Existing extensions in `DownloadCertificateDialog.razor.cs`: Der `.cer`, Pkcs12 `.pfx`, Pem `.pem`, Pkcs7 and Pkcs7Pem `.p7b`.

## Dependencies

(none)
