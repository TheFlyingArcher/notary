---
run: run-specsmd-fire-planner-6274df-001
work_item: loading-tracker-service
intent: loading-overlay
generated: 2026-11-08T03:05:00Z
---

# Code Review: Loading tracker service

## Auto-fixed

None needed. `dotnet format --verify-no-changes` is clean for the new files.

## Findings

No blocking issues. Notes:

- **Event ordering** — `Changed` is raised after the lock is released, so two quick transitions could reach subscribers out of order. The event carries no payload and subscribers read `IsLoading`, so they converge on the right state. Documented on the interface.
- **Threading** — `Changed` can fire on a timer thread. The overlay component (next work item) must use `InvokeAsync(StateHasChanged)`.
- **Stale timers** — a version counter makes a callback that was already running when its timer was cancelled a no-op.
- **Test flake** — `OpenSsl_OpensThePkcs12WithoutLegacyMode(RSA)` failed once in the parallel full run and passes alone; pre-existing and unrelated.

## Suggestions needing confirmation

None.
