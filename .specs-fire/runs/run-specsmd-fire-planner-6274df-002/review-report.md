---
run: run-specsmd-fire-planner-6274df-002
work_item: loading-overlay-component
intent: loading-overlay
generated: 2026-11-08T04:05:00Z
---

# Code Review: Loading overlay in the layout

## Auto-fixed

- `LoadingTracker` property initialised with `default!` so the change adds no new nullable warning. The existing `NavigationManager` property has the same warning; left alone.

## Findings

No blocking issues. Notes:

- **Dialogs** — a dialog is rendered outside the layout, so it is not made `inert`. The scrim covers it for the mouse, but Tab and Enter can still reach its own buttons. Follow-up for `wrap-service-calls`: disable dialog submit buttons while their call runs.
- **Focus** — when the layout turns `inert`, a focused control in it loses focus and does not get it back after the overlay clears. Worth checking in UAT.
- **Fire-and-forget** — `OnLoadingChanged` discards the `InvokeAsync` task; an exception inside the callback would be unobserved. The callback only reads a bool and calls `StateHasChanged`.
- **No automated test** — see the test report.

## Suggestions needing confirmation

None.
