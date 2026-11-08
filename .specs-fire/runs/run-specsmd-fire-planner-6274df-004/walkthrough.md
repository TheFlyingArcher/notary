---
run: run-specsmd-fire-planner-6274df-004
work_item: loading-overlay-docs-uat
intent: loading-overlay
generated: 2026-11-08T06:25:00Z
mode: autopilot
---

# Walkthrough: Loading overlay docs and UAT checklist

## Summary

Adds `docs/loading-overlay.md`, the guide for the loading overlay: how it works, how to wrap a new call, a ten-step UAT checklist for Visual Studio, and the checks to make on the Pi after deploy. This completes the loading-overlay intent.

## Files Changed

**Created**
- `docs/loading-overlay.md`

## Key Decisions

| Decision | Choice | Why |
|----------|--------|-----|
| Making a load slow for UAT | A temporary `Task.Delay(2000)` inside the `RunAsync` lambda | Local loads are too quick to see the overlay otherwise |
| README link | None | The README links only a few feature guides |

## Deviations from Plan

None.

## Dependencies Added

None.

## Verification

Read the guide against the code. UAT is yours to run from Visual Studio, following the checklist.

## Addendum: slow-call profile (after the run, at the maintainer's request)

UAT was hard on a fast machine, so a Development-only aid was added: `SimulatedLatencyLoadingTracker` (core library, 7 tests) delays every tracked call by `NOTARY_SIMULATED_LATENCY_MS`. `Program.cs` registers it only when the environment is Development and the value is above zero. A new **https (slow calls)** launch profile sets it to 1500. The guide's UAT section now uses the profile instead of hand-added delays.
