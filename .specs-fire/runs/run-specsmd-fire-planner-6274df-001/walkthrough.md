---
run: run-specsmd-fire-planner-6274df-001
work_item: loading-tracker-service
intent: loading-overlay
generated: 2026-11-08T03:10:00Z
mode: confirm
---

# Walkthrough: Loading tracker service

## Summary

Adds `ILoadingTracker`, a UI-agnostic counter of in-flight operations that decides when a loading overlay should show. It is registered scoped (one per Blazor circuit) but nothing uses it yet; the overlay component and the call-site wrapping are the next two work items.

## Structure

```
Page / dialog ──RunAsync(...)──▶ LoadingTracker ──Changed──▶ (overlay component, next item)
                                   │ count, show-delay timer, minimum-visible timer
                                   └── TimeProvider (system clock, or FakeTimeProvider in tests)
```

Pattern: a small stateful service behind an interface, injected by constructor. It lives in the core library (`Notary.Loading`) so `Notary.Test` can test it; the web project only registers it.

## Files Changed

**Created**
- `src/library/core/Loading/ILoadingTracker.cs` — contract: `IsLoading`, `Changed`, `Begin()`, two `RunAsync` overloads
- `src/library/core/Loading/LoadingTracker.cs` — the implementation
- `src/library/core/Loading/LoadingTrackerOptions.cs` — `ShowDelay` 200 ms, `MinimumVisible` 300 ms
- `src/test/LoadingTrackerTest.cs` — 18 tests

**Modified**
- `src/web/Program.cs` — registers `TimeProvider.System`, the default options and a scoped tracker
- `src/test/Notary.Test.csproj` — adds `Microsoft.Extensions.TimeProvider.Testing`

## Key Decisions

| Decision | Choice | Why |
|----------|--------|-----|
| Where it lives | Core library, not the web project | Tests reference the libraries but not the web project |
| Stale timer callbacks | Version counter checked under the lock | A callback already running when its timer is cancelled does nothing |
| Event payload | None; subscribers read `IsLoading` | Events raised outside the lock may reorder, but readers still converge |
| Timeout / watchdog | None | Settled at planning; a hung call is a separate bug |

## Deviations from Plan

None.

## Dependencies Added

- `Microsoft.Extensions.TimeProvider.Testing` 10.0.0 (test project only)

## Developer Notes

- `Changed` can fire on a timer thread. The overlay component must call `InvokeAsync(StateHasChanged)`.
- `FakeTimeProvider` hands the final time to a callback when one `Advance` spans several timers, so tests advance step by step.
- `OpenSsl_OpensThePkcs12WithoutLegacyMode(RSA)` failed once in the full parallel run and passed alone. It predates this change.

## Verification

- `dotnet test ./src/test/` — 731 of 732 passed in the full run; the one flake passes alone. The 18 new tests all pass.
- `dotnet build src/web/Notary.Web.csproj` succeeds.
- LoadingTracker line coverage: 92%.
- UAT: nothing visible changes yet. Start the web project from Visual Studio and confirm it still launches and signs in; the overlay appears with the next two items.
