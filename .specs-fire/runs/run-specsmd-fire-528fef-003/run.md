---
id: run-specsmd-fire-528fef-003
scope: single
work_items:
  - id: ca-validity-dropdown
    intent: validity-period-dropdown
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
current_item: null
status: completed
started: 2026-10-13T01:15:41.546Z
completed: 2026-10-13T01:17:56.095Z
---

# Run: run-specsmd-fire-528fef-003

## Scope
single (1 work item)

## Work Items
1. **ca-validity-dropdown** (confirm) — completed


## Current Item
(all completed)

## Files Created
(none)

## Files Modified
- `src/web/ViewModels/CreateCertificateAuthorityViewModel.cs`: LengthInMonths replaced by SelectedPeriod
- `src/web/Pages/CreateCertificateAuthority.razor`: Text field replaced by validity MudSelect
- `src/web/Pages/CreateCertificateAuthority.razor.cs`: Validity options, RefreshValidity, AddTo on submit

## Decisions
(none)


## Summary

- Work items completed: 1
- Files created: 0
- Files modified: 3
- Tests added: 0
- Coverage: 0%
- Completed: 2026-10-13T01:17:56.095Z
