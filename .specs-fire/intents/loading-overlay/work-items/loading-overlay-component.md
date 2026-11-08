---
id: loading-overlay-component
title: Loading overlay in the layout
intent: loading-overlay
complexity: medium
mode: confirm
status: completed
depends_on:
  - loading-tracker-service
created: 2026-11-07T00:00:00Z
run_id: run-specsmd-fire-planner-6274df-002
completed_at: 2026-11-08T17:46:57.891Z
---

# Work Item: Loading overlay in the layout

## Description

Add a `LoadingOverlay` component and render it in `MainLayout` so it covers the whole viewport, app bar and drawer included, while the tracker reports loading.

## Acceptance Criteria

- [ ] A translucent scrim (`MudOverlay`) covers the full viewport above all content, including the app bar and drawer
- [ ] A centered `MudPaper` card shows a `MudProgressCircular` and the text "Loading…"
- [ ] The overlay blocks pointer and keyboard interaction with the content beneath it
- [ ] It exposes `role="status"` and `aria-busy` so assistive technology is told the page is busy
- [ ] It reads correctly in light and dark mode
- [ ] The component unsubscribes from the tracker on dispose and re-renders via `InvokeAsync(StateHasChanged)`
- [ ] Rendered in `MainLayout.razor` for authenticated users; the login page is unaffected
- [ ] Build and all tests pass

## Technical Notes

Stock MudBlazor 9.5 only. Keep markup in `src/web/Shared/` alongside the other shared components. Check how `MudOverlay` handles z-index against the fixed `MudAppBar`.

## Dependencies

- loading-tracker-service
