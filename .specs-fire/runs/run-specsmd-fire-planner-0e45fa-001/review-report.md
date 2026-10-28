---
run: run-specsmd-fire-planner-0e45fa-001
work_item: download-format-catalog
intent: download-dialog-friendly-ux
generated: 2026-10-28T01:42:00Z
---

# Code Review: Download format catalog and file name builder

Files reviewed: `src/library/core/Download/CertificateDownloadOption.cs`, `src/library/core/Download/CertificateDownloadCatalog.cs`, `src/test/CertificateDownloadCatalogTest.cs`

## Auto-fixed

- Removed a redundant, always-true assertion at the top of `Blurb_MentionsPrivateKeyOnlyWhenItIsIncluded` before the first test run. Tests re-run green afterwards.

## Checked, no change needed

- Allman braces, file-scoped namespace, XML docs match `Notary.Validity`.
- Static class with no state; nothing to register in IoC.
- No path traversal: separators and invalid characters are removed, and leading dots are trimmed, so `..\..\x` becomes `x`.
- Nullable annotations are consistent (`string?` for user input).

## Suggestions (not applied)

- Windows reserved device names (`CON`, `NUL`, `COM1`) are not special-cased, so `CON` would produce `CON.cer`. The browser, not Notary, writes the file, and it handles this itself, so this is low priority.
- Blurbs are plain strings in the catalog. If the app is ever localized they would move to resources.

No security or architecture issues found.
