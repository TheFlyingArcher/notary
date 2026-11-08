---
run: run-specsmd-fire-planner-6274df-003
work_item: wrap-service-calls
intent: loading-overlay
mode: confirm
checkpoint: plan
approved_at: 2026-11-08T05:00:00Z
---

# Implementation Plan: Wrap page and dialog service calls

## Approach

Inject `ILoadingTracker` into each page and dialog that awaits a service and route those awaits through `RunAsync`. Behavior, results, error handling and snackbars stay as they are; the existing inline spinners and per-page `IsLoading` flags stay too (the overlay is additive).

Two rules:
- **Wrap whole load sequences, not each call.** A page that makes several calls in a row (CertificateDetail makes up to eight) would otherwise drop to zero in-flight between calls and restart the 200 ms show delay each time, so the overlay might never appear. One `RunAsync` around the sequence keeps the count above zero.
- **Never wrap anything that waits for the user.** `Dialog.ShowAsync`, `dialog.Result` and `ShowMessageBoxAsync` stay outside, or the overlay would cover the dialog the user needs to answer. Calls that are not backend services (JS interop, `LocalStorage`, `form.Validate`, `AuthProvider`) stay outside too.

Dialogs are not covered by the layout's `inert` (see the previous run's review), so `DownloadCertificateDialog` and `RevokeCertificateDialog` get a `_busy` flag: their OK and Cancel buttons are disabled and `OnOk` returns early while a call is running. This also stops a double click from submitting twice.

## Files to Create

(none)

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/Pages/CertificateAuthorities.razor.cs` | Inject tracker; wrap `CaService.GetAllAsync()` |
| `src/web/Pages/CertificateDetail.razor.cs` | Inject tracker; move the post-slug loading into `LoadAsync` and wrap it as one operation; wrap `ReinstateCertificateAsync` |
| `src/web/Pages/Certificates.razor.cs` | Inject tracker; wrap the first-render load (CA list, initial scope, certificates) as one operation; wrap the reload in `OnScopeChangedAsync` |
| `src/web/Pages/CreateCertificate.razor.cs` | Inject tracker; wrap `GetLeafIssuingAuthoritiesAsync` and `IssueCertificateAsync` |
| `src/web/Pages/CreateSelfSignedCertificate.razor.cs` | Inject tracker; wrap `IssueCertificateAsync` |
| `src/web/Pages/CreateCertificateAuthority.razor.cs` | Inject tracker; wrap the four `GetAsync` / `GetAllAsync` / `CreateAsync` calls |
| `src/web/Shared/DownloadCertificateDialog.razor(.cs)` | Inject tracker; wrap the fetch in `OnOk`; `_busy` guard and disabled buttons |
| `src/web/Shared/RevokeCertificateDialog.razor(.cs)` | Inject tracker; wrap `RevokeCertificateAsync`; `_busy` guard and disabled buttons |

Not changed: `CrlController` and `AuthenticateController` (HTTP endpoints, not UI), `Index` and `ShowProfile` (no service calls), `MainLayout` (already done).

## Tests

No new automated tests: the changes are call-site wiring in Razor code-behind, and the test project cannot reach the web project. Verification is the web build, the existing suite, and a search of `src/web/Pages` and `src/web/Shared` showing every service call is wrapped or deliberately excluded. The behavior is covered by the UAT checklist in the next work item.

## Technical Details

- Property: `[Inject] public ILoadingTracker LoadingTracker { get; set; } = default!;` in each, matching `MainLayout`.
- The existing `Task.Run(async () => ...)` wrappers stay; the tracker call goes around them.
- If a service call throws today, the page already behaves in some way (an unhandled circuit error in several places). `RunAsync` rethrows unchanged, so that behavior is not altered, only the overlay is guaranteed to clear.

---
*Plan approved at checkpoint. Execution follows.*
