---
run: run-specsmd-fire-planner-02371b-005
work_item: child-ca-own-key-settings
intent: mixed-chain-key-strength
generated: 2026-10-26T00:00:00Z
mode: confirm
---

# Implementation Walkthrough: Child CA own key settings

## Summary

A child CA now keeps the key algorithm, curve and length it was created with, and `CreateAsync` rejects a child whose key is weaker than its direct parent's. This is what makes mixed RSA/EC CA chains possible.

## Files Changed

### Modified

| File | Changes |
|------|---------|
| `src/library/service/CertificateAuthorityService.cs` | Removed the copy of the parent's key settings; added `KeyStrength.CheckNotWeaker(parent, child)` and a `ToKeySpec` helper |
| `src/test/CertificateAuthorityServiceTest.cs` | Valid key settings for two existing tests; 12 new test cases; `Setup` resets the shared mocks |

## Key Implementation Details

1. After the existing `CaProfileCatalog.CheckIssuer` call, the parent's and the child's key settings are turned into `KeySpec`s and compared. A failure returns the model's `Invalid` result before anything is issued or saved.
2. `ParentCaSlug` is still set from the parent; the child's algorithm, curve and length are left alone.
3. Roots skip the comparison. The "RSA needs a length" guard is unchanged.
4. Fail-closed: an unreadable parent or child key returns `Invalid`.

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Compare against | Direct parent only | A legacy violation higher up must not block new issuance |
| Unreadable parent key | Fail | Safer than guessing; parents always had these values |
| Error text | From `KeyStrength` | One message for CA and leaf checks |

## How to Verify

```bash
dotnet test ./src/test/ --filter "FullyQualifiedName~CertificateAuthorityServiceTest"
```

All 525 tests pass. The Create CA page still pre-fills the parent's settings, so it produces an equal-strength child; filtering the options is a later work item.
