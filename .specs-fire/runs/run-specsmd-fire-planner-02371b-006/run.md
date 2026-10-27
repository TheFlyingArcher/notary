---
id: run-specsmd-fire-planner-02371b-006
scope: single
work_items:
  - id: leaf-key-strength-floor
    intent: mixed-chain-key-strength
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
current_item: null
status: completed
started: 2026-10-27T00:20:04.187Z
completed: 2026-10-27T00:26:21.444Z
---

# Run: run-specsmd-fire-planner-02371b-006

## Scope
single (1 work item)

## Work Items
1. **leaf-key-strength-floor** (confirm) — completed


## Current Item
(all completed)

## Files Created
- `src/library/service/KeySpecs.cs`: Describe a BouncyCastle key as a KeySpec
- `src/test/LeafKeyStrengthTest.cs`: Leaf key strength floor tests

## Files Modified
- `src/library/service/CertificateService.cs`: Reject a certificate weaker than its issuing key
- `src/test/InMemoryFakes.cs`: FakeKeyService.Count
- `src/test/SignatureAlgorithmTest.cs`: RSA leaf under EC CA test uses a pairing the rule allows

## Decisions
(none)


## Summary

- Work items completed: 1
- Files created: 2
- Files modified: 3
- Tests added: 15
- Coverage: 0%
- Completed: 2026-10-27T00:26:21.444Z
