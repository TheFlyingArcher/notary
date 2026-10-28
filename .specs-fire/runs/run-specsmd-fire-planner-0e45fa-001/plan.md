---
run: run-specsmd-fire-planner-0e45fa-001
work_item: download-format-catalog
intent: download-dialog-friendly-ux
mode: confirm
checkpoint: plan
approved_at: 2026-10-28T01:30:00Z
---

# Implementation Plan: Download format catalog and file name builder

## Approach

Add a small static `CertificateDownloadCatalog` in the core library, in a new `Download` folder and namespace `Notary.Download`, following the existing `Validity` and `Profiles` pattern. It is a plain static class with no state, so nothing is registered in the IoC container.

For each `CertificateFormat` it returns an immutable `CertificateDownloadOption` record with these members:
- friendly label
- file extension
- blurb
- `RequiresPassword`
- `ContainsPrivateKey`

It also holds `BuildFileName(format, userName, certificateName)`:
- A blank input falls back to the certificate name.
- Characters from `Path.GetInvalidFileNameChars()`, plus `/` and `\`, are stripped.
- A trailing copy of the chosen extension (case-insensitive) is not doubled.
- A name that is empty after stripping falls back to the certificate name, and that fallback is sanitized the same way.

The next work item (`download-dialog-ui`) consumes this class. The dialog stays untouched here.

Blurb content (final wording is reviewable in the code):
- **Certificate only (.cer):** the public certificate in binary form. Use it for Windows import or most servers that want a single certificate. Contains no private key.
- **Certificate only, text (.pem):** the same public certificate as Base64 text. Use it for Linux, nginx, OpenSSL and pasting into config. Contains no private key.
- **Certificate + private key bundle (.pfx):** the certificate, its chain, and its private key in one password-protected file. Use it to install on a server or move to another machine. Keep the password safe. This is the only option that includes the private key.
- **Certificate chain, text (.p7b):** the certificate plus its issuing CAs as PEM text. Use it with `openssl pkcs7`, or to hand out a trust chain. Contains no private key.
- **Certificate chain, binary (.p7b):** the same chain in binary form. Use it for Windows import. OpenSSL needs `-inform DER`. Contains no private key.

## Files to Create

| File | Purpose |
|------|---------|
| `src/library/core/Download/CertificateDownloadOption.cs` | Record: format, label, extension, blurb, `RequiresPassword`, `ContainsPrivateKey` |
| `src/library/core/Download/CertificateDownloadCatalog.cs` | Static catalog (`All`, `For(format)`) and `BuildFileName` |
| `src/test/CertificateDownloadCatalogTest.cs` | NUnit tests for the catalog and file name builder |

## Files to Modify

| File | Changes |
|------|---------|
| (none) | The dialog is wired up in `download-dialog-ui` |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/CertificateDownloadCatalogTest.cs` | Every enum value has an option with a label, extension and blurb; the five exact labels in dialog order; only Pkcs12 requires a password and contains a key; the bundle blurb mentions the private key and no other blurb does; `For` rejects an undefined format; blank, whitespace and null input fall back to the certificate name; extension appended; no doubled extension (case-insensitive); invalid and path characters stripped; all-invalid input falls back; the certificate-name fallback is also sanitized |

## Technical Details

- **Deviation from the work item notes:** they suggested `src/web`. The test project references only core, service and data, not `Notary.Web`, so a class in `src/web` could not be tested without a new project reference. Core keeps it testable and matches `ValidityPolicy` and `CaProfileCatalog`.
- **Existing code:** `DownloadCertificateViewModel` already has `FileName`, so `download-dialog-ui` needs no view model change.
- **Style:** Allman braces, XML doc comments like `ValidityOption`, file-scoped namespaces like `Notary.Validity`, `ImplicitUsings` and `Nullable` per the project defaults.
- **Verification:** `dotnet build`, then `dotnet test ./src/test/` for the new tests plus the full suite.
- **UAT:** nothing visible yet. The dialog changes arrive in the next work item.

---
*Plan approved at checkpoint. Execution follows.*
