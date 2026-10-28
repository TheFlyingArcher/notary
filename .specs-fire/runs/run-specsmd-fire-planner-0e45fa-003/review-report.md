---
run: run-specsmd-fire-planner-0e45fa-003
work_item: download-dialog-ui
intent: download-dialog-friendly-ux
generated: 2026-10-28T02:52:00Z
---

# Code Review: Rework the download dialog

Files reviewed: `src/web/Shared/DownloadCertificateDialog.razor`, `src/web/Shared/DownloadCertificateDialog.razor.cs`

## Auto-fixed

- Added the missing `@using Notary.Download` to the razor file (build error), and moved the file's UTF-8 BOM back to the very first byte after the insert displaced it.

## Checked, no change needed

- A hidden password is not sent: `OnOk` passes the password only when `RequiresPassword`, and `OnFormatChanged` clears both fields.
- `SelectedOption` always resolves because the view model starts on the first catalog format; `For` would throw only for an undefined value, which the radios cannot produce.
- The password rules are unchanged apart from reading `RequiresPassword` instead of comparing to `Pkcs12`.
- The file name is passed through `BuildFileName`, which strips path and invalid characters, so a typed value cannot escape the download folder.

## Suggestions (not applied)

- The pre-existing `OnOk` has no try/catch, so a service failure (for example an expired certificate) still surfaces as an unhandled error. Out of scope for issue #82.
- Component tests (bUnit) would let the show/hide behavior be asserted automatically.

No security or architecture issues found.
