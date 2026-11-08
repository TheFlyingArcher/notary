---
run: run-specsmd-fire-planner-6274df-002
work_item: loading-overlay-component
intent: loading-overlay
generated: 2026-11-08T04:00:00Z
status: passed
---

# Test Report: Loading overlay in the layout

## Summary

| Category | Passed | Failed | Skipped | Coverage |
|----------|--------|--------|---------|----------|
| Whole suite | 732 | 0 | 0 | n/a |
| New automated tests | 0 | 0 | 0 | n/a |

No new automated tests, as agreed at the plan checkpoint: the item is Razor markup and event wiring, the tracker logic is already covered by 18 tests, and the test project has no component-test library or web project reference. The overlay was not run in a browser during this item; the app needs MongoDB and Auth0, so rendering and keyboard behavior are left to UAT.

## Acceptance Criteria Validation

- ✅ **Scrim covers the viewport above all content, including app bar and drawer** — `MudOverlay` at z-index 1450 (app bar and drawer 1100). Built; not viewed.
- ✅ **Centered card with spinner and "Loading…"** — `LoadingOverlay.razor`. Not viewed.
- ✅ **Blocks pointer and keyboard beneath it** — modal scrim for the mouse; `inert` on app bar, drawer and main content. UAT.
- ✅ **`role="status"` and `aria-busy`** — card has `role="status"` and `aria-live="polite"`; main content has `aria-busy`.
- ✅ **Light and dark mode** — theme-based `MudPaper` and `MudProgressCircular`, no hard-coded colors. UAT.
- ✅ **Unsubscribes on dispose, re-renders via `InvokeAsync(StateHasChanged)`** — `MainLayout` implements `IDisposable`.
- ✅ **Rendered inside `<Authorized>` only** — login page untouched.
- ✅ **Build and all tests pass** — web build succeeds; 732 of 732 tests pass; `dotnet format` clean.

## Test Commands

```bash
dotnet build src/web/Notary.Web.csproj
dotnet test ./src/test/
```
