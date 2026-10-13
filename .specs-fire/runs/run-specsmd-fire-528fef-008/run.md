---
id: run-specsmd-fire-528fef-008
scope: single
work_items:
  - id: ca-purpose-selector-ui
    intent: ca-purpose-profiles
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
current_item: null
status: completed
started: 2026-10-13T02:21:55.706Z
completed: 2026-10-13T02:25:40.461Z
---

# Run: run-specsmd-fire-528fef-008

## Scope
single (1 work item)

## Work Items
1. **ca-purpose-selector-ui** (confirm) — completed


## Current Item
(all completed)

## Files Created
- `src/library/core/Profiles/CaPurposeOption.cs`: Selector option record

## Files Modified
- `src/library/core/Profiles/CaProfileCatalog.cs`: Resolve, CheckIssuer, CheckCanIssueCas, EvaluateForParent
- `src/library/service/CertificateAuthorityService.cs`: Use shared catalog methods
- `src/web/ViewModels/CreateCertificateAuthorityViewModel.cs`: SelectedPurpose
- `src/web/Pages/CreateCertificateAuthority.razor`: Purpose selector, summary, Is Issuer, parent labels
- `src/web/Pages/CreateCertificateAuthority.razor.cs`: Options, refresh, Purpose on submit
- `src/test/CaProfileCatalogTest.cs`: 20 new tests

## Decisions
(none)


## Summary

- Work items completed: 1
- Files created: 1
- Files modified: 6
- Tests added: 20
- Coverage: 0%
- Completed: 2026-10-13T02:25:40.461Z
