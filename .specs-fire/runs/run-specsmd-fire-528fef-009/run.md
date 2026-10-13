---
id: run-specsmd-fire-528fef-009
scope: single
work_items:
  - id: leaf-profile-enforcement
    intent: ca-purpose-profiles
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
current_item: null
status: completed
started: 2026-10-13T02:44:28.493Z
completed: 2026-10-13T03:12:11.468Z
---

# Run: run-specsmd-fire-528fef-009

## Scope
single (1 work item)

## Work Items
1. **leaf-profile-enforcement** (confirm) — completed


## Current Item
(all completed)

## Files Created
(none)

## Files Modified
- `src/library/core/Profiles/CaProfileCatalog.cs`: Added CheckLeaf
- `src/library/core/Interface/Repository/ICertificateAuthorityRepository.cs`: Added GetByCertificateSlugAsync
- `src/library/data/Repository/CertificateAuthorityRepository.cs`: Implemented GetByCertificateSlugAsync
- `src/library/service/CertificateService.cs`: Leaf profile check before key generation
- `src/test/InMemoryFakes.cs`: CA repository fake query
- `src/test/CaProfileCatalogTest.cs`: CheckLeaf tests
- `src/test/CaPurposeIssuanceTest.cs`: Real-certificate leaf tests
- `src/test/CertificateRevokeServiceTest.cs`: Constructor argument

## Decisions
(none)


## Summary

- Work items completed: 1
- Files created: 0
- Files modified: 8
- Tests added: 27
- Coverage: 0%
- Completed: 2026-10-13T03:12:11.468Z
