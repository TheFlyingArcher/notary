---
run: run-specsmd-fire-planner-6274df-003
work_item: wrap-service-calls
intent: loading-overlay
generated: 2026-11-08T05:30:00Z
status: passed
---

# Test Report: Wrap page and dialog service calls

## Summary

| Category | Passed | Failed | Skipped | Coverage |
|----------|--------|--------|---------|----------|
| Whole suite | 732 | 0 | 0 | n/a |
| New automated tests | 0 | 0 | 0 | n/a |

No new automated tests, as agreed at the plan checkpoint: the changes are call-site wiring in Razor code-behind and the test project cannot reach the web project. The pages were not run in a browser; the app needs MongoDB and Auth0, so behavior is left to UAT.

## Acceptance Criteria Validation

- ✅ **Every awaited service call in `Pages` and `Shared` goes through the tracker** — a search for awaited `*Service.` / `*Svc.` calls leaves only calls inside a wrapped sequence (`CertificateDetail.LoadAsync` and its helpers; the `Certificates` first-render lambda and `LoadCertificatesAsync`, which is only called from wrapped paths) and `ShowMessageBoxAsync`, which waits on the user and is deliberately excluded
- ✅ **Reads, writes and downloads covered** — loads, issue certificate, create CA, reinstate, revoke, download
- ✅ **Behavior unchanged** — results, error handling and snackbars untouched; `RunAsync` rethrows as is
- ✅ **Tracker injected through constructors/`[Inject]`, not resolved ad hoc**
- ✅ **No unwrapped service call found by search** — see above
- ✅ **Build and existing tests pass** — web build succeeds; 732 of 732; `dotnet format` clean on the changed files

## Test Commands

```bash
dotnet build src/web/Notary.Web.csproj
dotnet test ./src/test/
```
