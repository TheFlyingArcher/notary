---
run: run-specsmd-fire-planner-6274df-002
work_item: loading-overlay-component
intent: loading-overlay
mode: confirm
checkpoint: plan
approved_at: 2026-11-08T03:30:00Z
---

# Implementation Plan: Loading overlay in the layout

## Approach

A presentational `LoadingOverlay` component draws the scrim and card; `MainLayout` owns the single subscription to `ILoadingTracker` and passes `Visible` down.

- **Overlay**: `MudOverlay` (`Modal`, `DarkBackground`) holding a `MudPaper` card with a large indeterminate `MudProgressCircular` and "Loading…". The card has `role="status"` and `aria-live="polite"` so the text is announced. Colors come from the MudBlazor theme, so light and dark mode follow the existing `_darkMode` switch.
- **Stacking**: z-index 1450, above the app bar, drawer and dialogs (1100 to 1400) and below snackbars (1500), so an open dialog is covered too and error toasts stay readable.
- **Blocking input**: the modal scrim blocks the mouse. For the keyboard, `MudAppBar`, `MudDrawer` and `MudMainContent` get the HTML `inert` attribute while loading, and `MudMainContent` gets `aria-busy`.
- **Layout wiring**: `MainLayout` implements `IDisposable`, subscribes to `Changed` in `OnInitialized`, re-renders through `InvokeAsync(StateHasChanged)` (the event can fire on a timer thread), ignores a disposed renderer, and unsubscribes on dispose.
- **Scope**: the overlay sits inside `<Authorized>`, so the login page is unaffected.

## Files to Create

| File | Purpose |
|------|---------|
| `src/web/Shared/LoadingOverlay.razor` | Scrim, spinner card and "Loading…" text, driven by a `Visible` parameter |

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/Shared/MainLayout.razor` | Render `<LoadingOverlay>`; `inert` and `aria-busy` bound to the loading state |
| `src/web/Shared/MainLayout.razor.cs` | Inject `ILoadingTracker`, subscribe and unsubscribe, expose `_loading`, implement `IDisposable` |

## Tests

The tracker logic is already covered. This item is markup and wiring only, and the test project has no component-test library (bUnit) or reference to the web project, so I plan no new automated tests. I will check the web project builds and the existing suite still passes. The visual and keyboard behavior goes on the UAT checklist in the last work item.

## Technical Details

- Known limit: inside an open dialog, Tab and Enter can still reach the dialog's own buttons while the overlay shows, because the dialog lives outside the layout. The next work item should disable submit buttons in dialogs while their call runs.
- Nothing yet wraps calls in the tracker, so the overlay will not appear until the next work item. To see it during UAT before then, temporarily wrap one call.
- Adding bUnit plus a web project reference is the alternative if you want automated component tests; I judge it too heavy for one small component.

---
*Plan approved at checkpoint. Execution follows.*
