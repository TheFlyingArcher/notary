---
run: run-specsmd-fire-planner-6274df-002
work_item: loading-overlay-component
intent: loading-overlay
generated: 2026-11-08T04:10:00Z
mode: confirm
---

# Walkthrough: Loading overlay in the layout

## Summary

`MainLayout` now listens to `ILoadingTracker` and shows a full-viewport overlay while it reports loading. Nothing wraps calls in the tracker yet, so the overlay does not appear until the next work item.

## Structure

```
LoadingTracker ──Changed──▶ MainLayout (_loading) ──Visible──▶ LoadingOverlay (MudOverlay + card)
                                   └── inert + aria-busy on app bar, drawer and main content
```

## Files Changed

**Created**
- `src/web/Shared/LoadingOverlay.razor` — modal `MudOverlay` (z-index 1450) with a `MudPaper` card holding a large spinner and "Loading…"; `role="status"`, `aria-live="polite"`

**Modified**
- `src/web/Shared/MainLayout.razor` — renders the overlay inside `<Authorized>`; `inert` on app bar, drawer and main content; `aria-busy` on main content
- `src/web/Shared/MainLayout.razor.cs` — injects the tracker, subscribes in `OnInitialized`, re-renders through `InvokeAsync(StateHasChanged)`, unsubscribes in `Dispose`

## Key Decisions

| Decision | Choice | Why |
|----------|--------|-----|
| Subscription owner | `MainLayout`; the overlay only takes `Visible` | One subscription, and the layout also needs the flag for `inert` |
| z-index | 1450 | Above dialogs (1400), below snackbars (1500) |
| Keyboard blocking | HTML `inert` on the layout regions | The modal scrim stops only the mouse |
| Tests | None new | Markup and wiring only; no bUnit available |

## Deviations from Plan

None.

## Dependencies Added

None.

## Developer Notes

- Dialogs are not covered by `inert`; see the review report.
- `MainLayout.razor` keeps its BOM and CRLF line endings.

## Verification

- `dotnet build src/web/Notary.Web.csproj` succeeds; `dotnet test ./src/test/` passes 732 of 732.
- UAT, before item 3: temporarily wrap one call (for example `CaService.GetAllAsync()` in `CertificateAuthorities.razor.cs`) in `LoadingTracker.RunAsync`, then add a short `Task.Delay`. Check the scrim and card appear after about 200 ms, in light and dark mode, that clicks and Tab do nothing behind it, and that it clears. Revert the temporary change.
