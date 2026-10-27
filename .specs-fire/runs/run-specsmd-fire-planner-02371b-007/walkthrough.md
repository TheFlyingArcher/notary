---
run: run-specsmd-fire-planner-02371b-007
work_item: child-ca-key-settings-ui
intent: mixed-chain-key-strength
generated: 2026-10-26T00:00:00Z
mode: confirm
---

# Implementation Walkthrough: Child CA key settings UI

## Summary

The Create Certificate Authority page now lets a child CA choose its own key, offering only algorithms, sizes and curves at least as strong as the parent. The key defaults to the parent's, so the common case is unchanged.

## Files Changed

### Created

| File | Purpose |
|------|---------|
| `src/library/core/Security/KeyOptions.cs` | The offered RSA lengths and curves, narrowed by an issuer's strength; reused by the leaf page next |
| `src/test/KeyOptionsTest.cs` | Unit tests for the filtering |

### Modified

| File | Changes |
|------|---------|
| `src/web/Pages/CreateCertificateAuthority.razor` | The key radios are no longer disabled under a parent; algorithm, size and curve options are filtered; an info note explains the floor |
| `src/web/Pages/CreateCertificateAuthority.razor.cs` | `ParentKey`, allowed options and `OnKeyTypeChanged` |

## Key Implementation Details

1. The filtering sits in `core` (`KeyOptions`) so it is unit tested; the page only reads it.
2. RSA is offered only if some RSA size qualifies, EC only if some curve qualifies. Under a P-384 parent that leaves Elliptic Curve alone.
3. Changing key type snaps the size or curve to the weakest allowed option so the form never holds a value below the floor.
4. Roots (no parent) show every option and no note. The server still enforces the rule and its error appears in the snackbar.

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Unavailable options | Hidden, with an explanatory note | The issue asks to offer only options that meet the floor |
| Where the logic lives | `core` | Testable and shared with the leaf page |
| Unreadable parent | Offer nothing | Fail closed |

## How to Verify

```bash
dotnet test ./src/test/ --filter "FullyQualifiedName~KeyOptionsTest"
dotnet build ./src/web
```

Then walk the UAT checklist in the test report in Visual Studio.
