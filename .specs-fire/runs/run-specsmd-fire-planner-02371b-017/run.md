---
id: run-specsmd-fire-planner-02371b-017
scope: single
work_items:
  - id: ca-issued-certificates-table
    intent: ca-detail-improvements
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
current_item: null
status: completed
started: 2026-10-30T00:38:07.846Z
completed: 2026-10-30T00:45:20.273Z
---

# Run: run-specsmd-fire-planner-02371b-017

## Scope
single (1 work item)

## Work Items
1. **ca-issued-certificates-table** (confirm) — completed


## Current Item
(all completed)

## Files Created
- `src/library/core/CertificateStatus.cs`: Status calculation
- `src/library/core/IssuedCertificateFilter.cs`: Root vs non-root issued list filter
- `src/web/ViewModels/IssuedCertificateRow.cs`: Table row model
- `src/test/CertificateStatusTest.cs`: Status tests
- `src/test/IssuedCertificateFilterTest.cs`: Filter tests

## Files Modified
- `src/web/Pages/CertificateDetail.razor`: Issued Certificates table
- `src/web/Pages/CertificateDetail.razor.cs`: Load issued certificates, download handler, shared status
- `src/web/ViewModels/CertificateViewModel.cs`: IssuedCertificates list

## Decisions
(none)


## Summary

- Work items completed: 1
- Files created: 5
- Files modified: 3
- Tests added: 10
- Coverage: 100%
- Completed: 2026-10-30T00:45:20.273Z
