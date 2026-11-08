---
run: run-specsmd-fire-planner-6274df-001
work_item: loading-tracker-service
intent: loading-overlay
mode: confirm
checkpoint: plan
approved_at: 2026-11-08T02:30:00Z
---

# Implementation Plan: Loading tracker service

## Approach

Add a UI-agnostic `ILoadingTracker` in the core library (next to `Changelog/`, which also holds UI-support logic) so it can be unit tested by `Notary.Test`, which references the libraries but not the web project. Blazor-specific code stays out; the web project only registers it.

Behavior:
- `Begin()` returns a disposable scope; `RunAsync(Func<Task>)` and `RunAsync<T>(Func<Task<T>>)` wrap it in try/finally so a throw or cancellation always ends the scope and the exception still reaches the caller.
- A lock guards the in-flight count. When the count goes 0 to 1, a show-delay timer (default 200 ms) starts. If it fires while the count is still above zero, `IsLoading` becomes true and `Changed` is raised.
- When the count returns to 0 before the delay elapses, the timer is cancelled and `IsLoading` never changes.
- When the count returns to 0 after the overlay was shown, it hides once it has been visible for at least the minimum (default 300 ms). A new operation starting during that wait cancels the hide and keeps the overlay up.
- Timers come from an injected `TimeProvider`; delays are in `LoadingTrackerOptions` (record with defaults). `Changed` is raised outside the lock.
- Disposing the scope twice is harmless.

## Files to Create

| File | Purpose |
|------|---------|
| `src/library/core/Loading/ILoadingTracker.cs` | Contract: `IsLoading`, `Changed`, `Begin`, two `RunAsync` overloads |
| `src/library/core/Loading/LoadingTrackerOptions.cs` | `ShowDelay` (200 ms) and `MinimumVisible` (300 ms) |
| `src/library/core/Loading/LoadingTracker.cs` | Implementation using `TimeProvider` timers |
| `src/test/LoadingTrackerTest.cs` | Unit tests |

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/Program.cs` | `TryAddSingleton(TimeProvider.System)`, `AddScoped<ILoadingTracker, LoadingTracker>()` (one tracker per Blazor circuit), default options |
| `src/test/Notary.Test.csproj` | Add `Microsoft.Extensions.TimeProvider.Testing` (Microsoft package) for `FakeTimeProvider` |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/LoadingTrackerTest.cs` | Starts not loading; operation shorter than the delay never raises it; longer than the delay raises it exactly once; overlapping operations keep it on until the last ends; exception and cancellation still decrement and propagate; minimum visible time is honored; a new operation during the hide wait keeps it on; `Changed` fires on each transition only; double dispose is safe; `RunAsync<T>` returns the value |

## Technical Details

- Scoped lifetime gives one tracker per circuit, as the brief requires.
- Subscribers must marshal to the render thread (`InvokeAsync(StateHasChanged)`); that is the next work item.
- No timeout or watchdog (decided at planning).
- Existing tests are untouched; the only shared-file change is the added test package.

---
*Plan approved at checkpoint. Execution follows.*
