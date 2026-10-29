---
run: run-specsmd-fire-planner-0e45fa-003
work_item: download-dialog-ui
intent: download-dialog-friendly-ux
mode: confirm
checkpoint: plan
approved_at: 2026-10-28T02:30:00Z
---

# Implementation Plan: Rework the download dialog

## Approach

Drive the dialog from `CertificateDownloadCatalog` instead of hard-coded markup.

- **Radios:** render one `MudRadio` per `CertificateDownloadCatalog.All` entry, in catalog order, labelled with the friendly label.
- **Default selection:** today nothing is selected when the dialog opens (the enum default is 0), so pressing OK fails with an unhandled `ArgumentException` from the service. Preselect the first option, "Certificate only (.cer)", so the blurb always has something to describe.
- **Blurb:** a `MudAlert` under the form shows the selected option's blurb and changes with the radio. It is `Severity.Warning` for the private key bundle (it is the one with a key) and `Severity.Info` for the rest.
- **Passwords:** render the Password and Confirm Password fields only when the selected option `RequiresPassword`. The radio's `ValueChanged` clears both fields on any change, so a hidden password is never submitted or left in memory. In `OnOk` the password is passed only when required.
- **File name:** an optional `MudTextField` with label "File name (optional)", helper text "Leave blank to use the certificate name. The extension is added for you.", and `MaxLength="100"`. A blank value uses the certificate `Name`.
- **Download name:** replace the extension `switch` in `OnOk` with `CertificateDownloadCatalog.BuildFileName(format, ViewModel.FileName, cert.Name)`.
- **Password rules:** unchanged (8+ characters, upper, lower, digit; must match). The validators use the catalog's `RequiresPassword` instead of comparing to `Pkcs12`.

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/Shared/DownloadCertificateDialog.razor` | Catalog-driven radios, blurb alert, conditional password fields, file name box |
| `src/web/Shared/DownloadCertificateDialog.razor.cs` | Default format, format-change handler, selected-option property, catalog file name, validators use `RequiresPassword` |

`DownloadCertificateViewModel` already has `FileName`, so it is unchanged. The three pages that open the dialog (`/ca`, `/certificates`, certificate detail) are unchanged.

## Tests

The repo has no Blazor component test project, and adding bUnit is out of scope. The logic that matters (labels, blurbs, password flags, file names) is already covered by the 32 catalog tests from run 001. This item is checked by the full test suite, a Release build of `Notary.Web`, and your UAT.

## Technical Details

- **UAT in Visual Studio**, from `/ca`, `/certificates` and a certificate detail page:
  1. Open Download: the first radio is selected and a blurb shows; no password fields.
  2. Click each radio: the blurb changes; the password fields appear only for "Certificate + private key bundle (.pfx)", and that blurb is the yellow warning.
  3. Pick the bundle, type a password, switch away and back: the fields are empty.
  4. Download with the file name blank: the file is named after the certificate with the right extension.
  5. Download with `my cert`, with `my-cert.pem` on the PEM option, and with `a/b:c`: you get `my cert.<ext>`, `my-cert.pem` (not doubled) and `abc.<ext>`.
  6. The bundle opens with its password; the PEM chain opens with plain `openssl pkcs7`.
- **Risk:** MudForm validity can be stale after fields are removed; `OnOk` already calls `_form.Validate()` first, and the plan keeps that.

---
*Plan approved at checkpoint. Execution follows.*
