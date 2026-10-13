---
id: run-specsmd-fire-528fef-006
scope: single
work_items:
  - id: ca-purpose-persistence
    intent: ca-purpose-profiles
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
current_item: null
status: completed
started: 2026-10-13T02:06:29.857Z
completed: 2026-10-13T02:09:16.583Z
---

# Run: run-specsmd-fire-528fef-006

## Scope
single (1 work item)

## Work Items
1. **ca-purpose-persistence** (confirm) — completed


## Current Item
(all completed)

## Files Created
- `src/test/CaPurposePersistenceTest.cs`: Mapping, BSON and JSON tests

## Files Modified
- `src/library/core/Contract/CertificateAuthority.cs`: Added Purpose and custom usages
- `src/library/data/Model/CertificateAuthorityModel.cs`: Added purpose and custom usage elements
- `src/library/data/ModelMapProfile.cs`: AfterMap keeps unset custom lists null

## Decisions
(none)


## Summary

- Work items completed: 1
- Files created: 1
- Files modified: 3
- Tests added: 9
- Coverage: 0%
- Completed: 2026-10-13T02:09:16.583Z
