---
id: run-specsmd-fire-528fef-010
scope: single
work_items:
  - id: leaf-profile-ui
    intent: ca-purpose-profiles
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
current_item: null
status: completed
started: 2026-10-13T03:13:31.526Z
completed: 2026-10-13T03:19:32.883Z
---

# Run: run-specsmd-fire-528fef-010

## Scope
single (1 work item)

## Work Items
1. **leaf-profile-ui** (confirm) — completed


## Current Item
(all completed)

## Files Created
(none)

## Files Modified
- `src/library/core/Profiles/CaProfile.cs`: Added LocksLeafUsages
- `src/web/ViewModels/CreateCertificateViewModel.cs`: Added ResetUsages
- `src/web/Pages/CreateCertificate.razor`: Pickers hidden for purpose CAs; usage note; issuer labels
- `src/web/Pages/CreateCertificate.razor.cs`: Lock and unlock usages; filter issuers; usage note
- `src/test/CaProfileCatalogTest.cs`: LocksLeafUsages tests

## Decisions
(none)


## Summary

- Work items completed: 1
- Files created: 0
- Files modified: 5
- Tests added: 8
- Coverage: 0%
- Completed: 2026-10-13T03:19:32.883Z
