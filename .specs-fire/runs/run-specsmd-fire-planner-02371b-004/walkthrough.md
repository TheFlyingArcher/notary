---
run: run-specsmd-fire-planner-02371b-004
work_item: key-strength-model
intent: mixed-chain-key-strength
generated: 2026-10-26T00:00:00Z
mode: confirm
---

# Implementation Walkthrough: Key strength model

## Summary

Added the shared model behind the mixed-chain rule: a NIST SP 800-57 security-bits table for RSA and EC keys, and a check that a subject key is no weaker than its issuer's. Nothing calls it yet; the CA, leaf and UI work items build on it.

## Files Changed

### Created

| File | Purpose |
|------|---------|
| `src/library/core/Security/KeyStrength.cs` | `KeySpec` record plus `KeyStrength.GetSecurityBits`, `CheckNotWeaker` and `MeetsFloor` |
| `src/test/KeyStrengthTest.cs` | 31 tests for the table, comparisons and failures |

### Modified

(none)

## Key Implementation Details

1. `GetSecurityBits(KeySpec)` returns `Result<int>`. EC maps by curve; RSA maps by length with a switch on bands, so off-table sizes take the row below (2560 is 112 bits).
2. `CheckNotWeaker(issuer, subject)` returns `Result` with `ResultStatus.Invalid` and a message such as "A 112-bit key cannot be issued under a 128-bit CA". Equal strength passes across key types.
3. `MeetsFloor(issuer, candidate)` is a `bool` form for the UI to filter options; unreadable keys are not offered.

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Home | `Notary.Security` in `core` | Services, web pages and tests all share it |
| Input type | `KeySpec` record | One shape for CA and key data; the callers map to it |
| Unknown algorithm | Failure, not a default | A new algorithm must be given a strength deliberately |

## How to Verify

```bash
dotnet test ./src/test/ --filter "FullyQualifiedName~KeyStrengthTest"
```

All 513 tests pass.
