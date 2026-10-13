---
id: run-specsmd-fire-planner-01019e-001
scope: single
work_items:
  - id: certificates-by-ca-listing
    intent: organize-certificates-by-ca
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
current_item: null
status: completed
started: 2026-10-13T23:26:38.109Z
completed: 2026-10-13T23:29:35.943Z
---

# Run: run-specsmd-fire-planner-01019e-001

## Scope
single (1 work item)

## Work Items
1. **certificates-by-ca-listing** (confirm) — completed


## Current Item
(all completed)

## Files Created
- `src/test/CertificateListingTest.cs`: Tests for the CA, per-CA and self-signed listing queries

## Files Modified
- `src/library/core/Interface/Repository/ICertificateRepository.cs`: Added GetSelfSignedAsync
- `src/library/core/Interface/Service/ICertificateService.cs`: Added GetSelfSignedCertificatesAsync
- `src/library/core/Interface/Service/ICertificateAuthorityService.cs`: Added GetLeafIssuingAuthoritiesAsync
- `src/library/data/Repository/CertificateRepository.cs`: Self-signed certificate filter
- `src/library/service/CertificateService.cs`: Self-signed listing
- `src/library/service/CertificateAuthorityService.cs`: Leaf-issuing non-revoked CA listing
- `src/test/InMemoryFakes.cs`: Fake GetSelfSignedAsync

## Decisions
(none)


## Summary

- Work items completed: 1
- Files created: 1
- Files modified: 7
- Tests added: 7
- Coverage: 0%
- Completed: 2026-10-13T23:29:35.943Z
