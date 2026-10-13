---
id: run-specsmd-fire-528fef-007
scope: single
work_items:
  - id: ca-cert-from-profile
    intent: ca-purpose-profiles
    mode: validate
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
current_item: null
status: completed
started: 2026-10-13T02:10:30.841Z
completed: 2026-10-13T02:14:38.185Z
---

# Run: run-specsmd-fire-528fef-007

## Scope
single (1 work item)

## Work Items
1. **ca-cert-from-profile** (validate) — completed


## Current Item
(all completed)

## Files Created
- `src/test/InMemoryFakes.cs`: Shared in-memory fakes
- `src/test/CaPurposeIssuanceTest.cs`: Real-certificate CA issuance tests

## Files Modified
- `src/library/core/Contract/CertificateRequest.cs`: Optional PathLengthConstraint
- `src/library/service/CertificateService.cs`: Validate and emit path length
- `src/library/service/CertificateAuthorityService.cs`: Profile-driven CA requests and checks
- `src/test/CertificateRevokeServiceTest.cs`: Use shared fakes

## Decisions
(none)


## Summary

- Work items completed: 1
- Files created: 2
- Files modified: 4
- Tests added: 18
- Coverage: 0%
- Completed: 2026-10-13T02:14:38.185Z
