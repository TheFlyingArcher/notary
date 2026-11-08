---
id: loading-tracker-service
title: Loading tracker service
intent: loading-overlay
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-11-07T00:00:00Z
run_id: run-specsmd-fire-planner-6274df-001
completed_at: 2026-11-08T02:20:10.726Z
---

# Work Item: Loading tracker service

## Description

Add an `ILoadingTracker` to the web project that counts in-flight operations and tells the UI when to show the overlay. Operations are wrapped with `RunAsync(...)` (or a disposable scope) so a throw or cancellation always decrements the count. Timing uses an injected `TimeProvider` so it can be tested.

## Acceptance Criteria

- [ ] `ILoadingTracker` exposes `IsLoading`, a change event, and `RunAsync` overloads for `Task` and `Task<T>`
- [ ] Overlapping operations are counted; `IsLoading` stays true until the last one finishes
- [ ] The count is decremented when an operation throws or is cancelled, and the exception still reaches the caller
- [ ] `IsLoading` becomes true only after about 200 ms of continuous activity; operations shorter than that never raise it
- [ ] Once shown, `IsLoading` stays true for at least about 300 ms
- [ ] Registered as scoped in `Program.cs`; uses `TimeProvider`, constructor injection, Allman style
- [ ] Unit tests cover counting, overlap, exceptions, cancellation, the delay and the minimum visible time; build and all tests pass

## Technical Notes

Scoped lifetime means one tracker per Blazor circuit, which is what we want. Raise the change event so subscribers can call `InvokeAsync(StateHasChanged)`. No timeout or watchdog in this item (see brief).

## Dependencies

(none)
