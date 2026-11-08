---
run: run-specsmd-fire-planner-6274df-003
work_item: wrap-service-calls
intent: loading-overlay
generated: 2026-11-08T05:40:00Z
mode: confirm
---

# Walkthrough: Wrap page and dialog service calls

## Summary

Every page and dialog that awaits a backend service now does so through `ILoadingTracker`, so the overlay from the previous item appears for page loads, creates, reinstates, revokes and downloads. The loading intent is now functionally complete; the last item documents it and the UAT checks.

## Structure

```
Page / dialog ──LoadingTracker.RunAsync(...)──▶ service call ──▶ (tracker counts it; MainLayout shows the overlay)
```

## Files Changed

**Modified**
- `Pages/CertificateAuthorities.razor.cs` — wraps `GetAllAsync`
- `Pages/CertificateDetail.razor.cs` — the post-slug loading moved into `LoadAsync` and wrapped as one operation; wraps `ReinstateCertificateAsync`
- `Pages/Certificates.razor.cs` — wraps the first-render load (CA list, scope, certificates) as one operation, and the reload in `OnScopeChangedAsync`
- `Pages/CreateCertificate.razor.cs`, `Pages/CreateSelfSignedCertificate.razor.cs` — wrap the eligible-CA load and `IssueCertificateAsync`
- `Pages/CreateCertificateAuthority.razor.cs` — wraps its four service calls
- `Shared/DownloadCertificateDialog.razor(.cs)`, `Shared/RevokeCertificateDialog.razor(.cs)` — wrap the service calls; `_busy` flag disables OK and Cancel and ignores a second click

## Key Decisions

| Decision | Choice | Why |
|----------|--------|-----|
| Granularity | Wrap whole load sequences | Sequential calls would otherwise restart the 200 ms delay each time |
| User waits | Never wrapped | The overlay would cover the dialog the user must answer |
| Dialog guard | `_busy` flag | Dialogs are outside the layout the overlay makes `inert` |
| Existing spinners | Kept | Overlay is additive; separate cleanup |

## Deviations from Plan

None.

## Dependencies Added

None.

## Developer Notes

- New pages or dialogs that call a service need `[Inject] ILoadingTracker` and a `RunAsync` around the call; the docs item will say so.
- `Task.Run` wrappers were left in place; the tracker call goes around them.

## Verification

- `dotnet build src/web/Notary.Web.csproj` succeeds; `dotnet test ./src/test/` passes 732 of 732.
- UAT (from Visual Studio): open Certificate Authorities, Certificates and a certificate's detail page and watch for the overlay on a slow load (add a temporary `Task.Delay` in a service or throttle MongoDB if no load is slow enough). Create a certificate and a CA; revoke and download from the dialogs and confirm OK and Cancel grey out while the call runs. The full checklist comes with the docs item.
