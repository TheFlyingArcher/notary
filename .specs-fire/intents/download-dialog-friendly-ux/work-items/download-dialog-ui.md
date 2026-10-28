---
id: download-dialog-ui
title: Rework the download dialog
intent: download-dialog-friendly-ux
complexity: medium
mode: confirm
status: completed
depends_on:
  - download-format-catalog
created: 2026-10-28T00:00:00Z
run_id: run-specsmd-fire-planner-0e45fa-003
completed_at: 2026-10-28T06:07:31.391Z
---

# Work Item: Rework the download dialog

## Description

Update `DownloadCertificateDialog` to use the format catalog: friendly radio labels, a blurb for the selected format, password fields shown only for the private key bundle, and an optional file name box.

## Acceptance Criteria

- [ ] The five radios show the friendly labels from the catalog, in the existing order
- [ ] A blurb below the form updates when the selected radio changes
- [ ] Password and Confirm Password are not rendered unless the private key bundle (PKCS #12) is selected; their validation still applies when shown and does not block other formats
- [ ] An optional "File name" text box is blank by default; hint text says a blank value uses the certificate name and that the extension is added automatically
- [ ] The downloaded file name comes from the catalog builder, replacing the `switch` in `OnOk`
- [ ] Download works for every format from `/ca`, `/certificates` and the certificate detail page
- [ ] Solution builds cleanly and existing tests pass

## Technical Notes

Files: `src/web/Shared/DownloadCertificateDialog.razor` and `.razor.cs`, `src/web/ViewModels/DownloadCertificateViewModel.cs` (add `FileName`). Switching away from the bundle should clear the password fields. UAT in Visual Studio: pick each radio and watch the blurb and password fields; download with a blank file name and with a custom one including an invalid character; confirm the PKCS #12 file still opens with its password.

## Dependencies

- download-format-catalog
