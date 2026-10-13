---
id: run-specsmd-fire-planner-01019e-003
scope: single
work_items:
  - id: create-certificate-scoped-to-ca
    intent: organize-certificates-by-ca
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
current_item: null
status: completed
started: 2026-10-13T23:37:17.775Z
completed: 2026-10-13T23:38:44.011Z
---

# Run: run-specsmd-fire-planner-01019e-003

## Scope
single (1 work item)

## Work Items
1. **create-certificate-scoped-to-ca** (confirm) — completed


## Current Item
(all completed)

## Files Created
(none)

## Files Modified
- `src/web/Pages/CreateCertificate.razor`: Read-only Issuing CA field, form renders after CA applied
- `src/web/Pages/CreateCertificate.razor.cs`: ca query parameter, eligibility check and redirect, ApplyCa
- `src/web/ViewModels/CreateCertificateViewModel.cs`: Removed CertificateAuthoritySlug and its event

## Decisions
(none)


## Summary

- Work items completed: 1
- Files created: 0
- Files modified: 3
- Tests added: 0
- Coverage: 0%
- Completed: 2026-10-13T23:38:44.011Z
