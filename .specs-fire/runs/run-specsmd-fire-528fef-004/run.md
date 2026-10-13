---
id: run-specsmd-fire-528fef-004
scope: single
work_items:
  - id: leaf-validity-dropdown
    intent: validity-period-dropdown
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
current_item: null
status: completed
started: 2026-10-13T01:20:10.340Z
completed: 2026-10-13T01:21:48.591Z
---

# Run: run-specsmd-fire-528fef-004

## Scope
single (1 work item)

## Work Items
1. **leaf-validity-dropdown** (confirm) — completed


## Current Item
(all completed)

## Files Created
(none)

## Files Modified
- `src/web/ViewModels/CreateCertificateViewModel.cs`: ExpiryLength replaced by SelectedPeriod
- `src/web/Pages/CreateCertificate.razor`: Text field replaced by validity MudSelect
- `src/web/Pages/CreateCertificate.razor.cs`: Validity options, RefreshValidity, AddTo on submit, removed ValidateExpiration

## Decisions
(none)


## Summary

- Work items completed: 1
- Files created: 0
- Files modified: 3
- Tests added: 0
- Coverage: 0%
- Completed: 2026-10-13T01:21:48.591Z
