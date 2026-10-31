---
id: run-notary-001
scope: batch
work_items:
  - id: ci-change-detection
    intent: selective-ci-runs
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
  - id: codeql-advanced-workflow
    intent: selective-ci-runs
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
current_item: null
status: completed
started: 2026-10-31T20:44:06.756Z
completed: 2026-10-31T21:06:50.947Z
---

# Run: run-notary-001

## Scope
batch (2 work items)

## Work Items
1. **ci-change-detection** (confirm) — completed
2. **codeql-advanced-workflow** (confirm) — completed


## Current Item
(all completed)

## Files Created
- `.github/scripts/detect-dotnet-changes.sh`: Shared path-based change detection
- `.github/workflows/codeql.yml`: Path-gated CodeQL scan for C#

## Files Modified
- `.github/workflows/ci.yml`: Added changes job; gated quality and release on it

## Decisions
(none)


## Summary

- Work items completed: 2
- Files created: 2
- Files modified: 1
- Tests added: 17
- Coverage: 0%
- Completed: 2026-10-31T21:06:50.947Z
