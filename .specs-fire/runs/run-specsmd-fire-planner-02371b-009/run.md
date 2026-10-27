---
id: run-specsmd-fire-planner-02371b-009
scope: single
work_items:
  - id: leaf-key-settings-ui
    intent: mixed-chain-key-strength
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
current_item: null
status: completed
started: 2026-10-27T00:57:52.583Z
completed: 2026-10-27T01:00:55.055Z
---

# Run: run-specsmd-fire-planner-02371b-009

## Scope
single (1 work item)

## Work Items
1. **leaf-key-settings-ui** (confirm) — completed


## Current Item
(all completed)

## Files Created
(none)

## Files Modified
- `src/web/Pages/CreateCertificate.razor`: Editable key section filtered by the issuing CA strength, with labels, chips and a note
- `src/web/Pages/CreateCertificate.razor.cs`: Issuer key, allowed options, label helpers and OnKeyAlgorithmChanged
- `src/library/core/Security/KeyOptions.cs`: Wording says issuing CA
- `src/test/KeyOptionsTest.cs`: Updated expected wording

## Decisions
(none)


## Summary

- Work items completed: 1
- Files created: 0
- Files modified: 4
- Tests added: 0
- Coverage: 0%
- Completed: 2026-10-27T01:00:55.055Z
