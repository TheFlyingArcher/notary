---
run: run-specsmd-fire-planner-6274df-001
work_item: loading-tracker-service
intent: loading-overlay
generated: 2026-11-08T03:00:00Z
status: passed
---

# Test Report: Loading tracker service

## Summary

| Category | Passed | Failed | Skipped | Coverage |
|----------|--------|--------|---------|----------|
| Unit (LoadingTrackerTest) | 18 | 0 | 0 | 92% (LoadingTracker lines) |
| Whole suite | 731 | 1* | 0 | n/a |

\* `OpenSsl_OpensThePkcs12WithoutLegacyMode(RSA)` failed once under the full parallel run and passed on its own (2 of 2). It is an existing test that shells out to OpenSSL and is unrelated to this change; flagged here, not changed.

## Acceptance Criteria Validation

- ✅ **ILoadingTracker exposes IsLoading, change event, RunAsync for Task and Task<T>** (also `Begin()`)
- ✅ **Overlapping operations are counted** — `Overlapping_Operations_KeepLoadingUntilTheLastEnds`
- ✅ **Count decremented on throw/cancel; exception reaches caller** — `RunAsync_Throws_*`, `RunAsync_Cancelled_*`, `RunAsync_OperationThrowsSynchronously_*`
- ✅ **About 200 ms show delay** — `Begin_BeforeShowDelay_*`, `ShortOperation_NeverShows*`
- ✅ **About 300 ms minimum visible time** — `Overlay_StaysForTheMinimumVisibleTime`, `NewOperation_DuringMinimumVisibleWait_*`
- ✅ **Scoped registration in Program.cs, TimeProvider, constructor injection, Allman** — web project builds; `dotnet format --verify-no-changes` clean
- ✅ **Unit tests; build and all tests pass** — 18 new tests pass; the one unrelated flake is noted above

## Tests Written

### Unit Tests

- `src/test/LoadingTrackerTest.cs` — idle state, default options, show delay, short calls, overlap, minimum visible time, restart during hide wait, double dispose, `RunAsync` value/exception/cancel/sync-throw, null arguments, dispose stops timers

## Test Commands

```bash
dotnet test ./src/test/
dotnet test ./src/test/ --filter "FullyQualifiedName~LoadingTrackerTest"
dotnet test ./src/test/ --collect:"XPlat Code Coverage"
```
