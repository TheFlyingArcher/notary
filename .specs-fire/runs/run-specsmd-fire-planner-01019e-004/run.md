---
id: run-specsmd-fire-planner-01019e-004
scope: single
work_items:
  - id: self-signed-certificate-form
    intent: organize-certificates-by-ca
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
current_item: null
status: completed
started: 2026-10-13T23:42:14.251Z
completed: 2026-10-13T23:44:23.381Z
---

# Run: run-specsmd-fire-planner-01019e-004

## Scope
single (1 work item)

## Work Items
1. **self-signed-certificate-form** (confirm) — completed


## Current Item
(all completed)

## Files Created
- `src/web/Pages/CreateSelfSignedCertificate.razor`: Self-signed certificate form
- `src/web/Pages/CreateSelfSignedCertificate.razor.cs`: Form logic and submit
- `src/web/Shared/CertificateFormRules.cs`: Shared name validators

## Files Modified
- `src/web/Pages/CreateCertificate.razor`: Use shared validators
- `src/web/Pages/CreateCertificate.razor.cs`: Removed local validators
- `src/test/CaPurposeIssuanceTest.cs`: Added self-signed leaf test

## Decisions
(none)


## Summary

- Work items completed: 1
- Files created: 3
- Files modified: 3
- Tests added: 1
- Coverage: 0%
- Completed: 2026-10-13T23:44:23.381Z
