---
run: run-specsmd-fire-planner-02371b-010
work_item: key-strength-tests-docs
intent: mixed-chain-key-strength
generated: 2026-10-26T00:00:00Z
mode: confirm
---

# Implementation Walkthrough: Key strength tests and docs

## Summary

Closed the intent with end-to-end tests over real mixed chains and a document describing the rule. No production code changed.

## Files Changed

### Created

| File | Purpose |
|------|---------|
| `src/test/MixedChainTest.cs` | Real RSA/EC chains and the issuer/subject matrix, for CAs and leaves |
| `docs/key-strength.md` | The rule, the strength table, where it is enforced and how the pages behave |

### Modified

| File | Changes |
|------|---------|
| `src/test/CertificateAuthorityServiceTest.cs` | Legacy case: a stronger grandparent is never consulted |
| `README.md` | Link to the new document |

## Key Implementation Details

1. Two chains are built through the real services: RSA-3072 root, EC P-256 intermediate, RSA and EC leaves; and an EC P-384 chain with a P-521 leaf. Every certificate verifies and is signed with its issuer's algorithm.
2. A 13-case matrix asserts that a CA and a leaf are both accepted or both rejected, and that a rejection creates nothing.
3. The doc explains why RSA-3072 and RSA-4096 count as the same level.

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| RSA sizes in tests | 2048 and 3072 | Keeps the suite fast; larger sizes are covered by the model's unit tests |
| Legacy coverage | Direct-issuer-only test | Matches the agreed rule that old chains keep working |

## How to Verify

```bash
dotnet test ./src/test/
```

All 578 tests pass.
