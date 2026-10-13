---
id: run-specsmd-fire-528fef-011
scope: single
work_items:
  - id: custom-ca-usages-ui
    intent: ca-purpose-profiles
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
current_item: null
status: completed
started: 2026-10-13T03:23:57.985Z
completed: 2026-10-13T03:28:31.320Z
---

# Run: run-specsmd-fire-528fef-011

## Scope
single (1 work item)

## Work Items
1. **custom-ca-usages-ui** (confirm) — completed


## Current Item
(all completed)

## Files Created
(none)

## Files Modified
- `src/library/core/Profiles/KeyPurposeOids.cs`: Known list and GetName
- `src/library/core/Profiles/CaProfileCatalog.cs`: Custom offered under restricted parent; DescribeKeyUsage public
- `src/web/ViewModels/CreateCertificateAuthorityViewModel.cs`: Selected custom usages
- `src/web/Pages/CreateCertificateAuthority.razor`: Custom usage pickers
- `src/web/Pages/CreateCertificateAuthority.razor.cs`: Custom option rules, validation, save
- `src/test/CaProfileCatalogTest.cs`: Custom and name tests
- `src/test/CaPurposeIssuanceTest.cs`: Real-certificate Custom tests

## Decisions
(none)


## Summary

- Work items completed: 1
- Files created: 0
- Files modified: 7
- Tests added: 12
- Coverage: 0%
- Completed: 2026-10-13T03:28:31.320Z
