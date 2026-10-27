---
run: run-specsmd-fire-planner-02371b-009
work_item: leaf-key-settings-ui
intent: mixed-chain-key-strength
generated: 2026-10-26T00:00:00Z
mode: confirm
---

# Implementation Walkthrough: Leaf key settings UI

## Summary

The Create Certificate page now offers only key options at least as strong as the issuing CA, with the same security-level labels and neutral chips as the Create CA page. The key defaults to the CA's own, and a user can choose a different type or size that meets the floor.

## Files Changed

### Modified

| File | Changes |
|------|---------|
| `src/web/Pages/CreateCertificate.razor` | Radios are no longer disabled under a CA; filtered algorithms, sizes and curves with labels and chips; issuer line and note |
| `src/web/Pages/CreateCertificate.razor.cs` | `IssuerKey`, allowed options, label helpers, `OnKeyAlgorithmChanged` |
| `src/library/core/Security/KeyOptions.cs` | The shared wording says "issuing CA" |
| `src/test/KeyOptionsTest.cs` | Updated expected wording |

## Key Implementation Details

1. The page reuses `KeyOptions` for filtering, labels, relations and the note, so it can never offer something `MeetsFloor` rejects.
2. Switching key type snaps the size or curve into the allowed set and refreshes the usages (EC cannot encipher; purpose CAs set usages per key type).
3. Server rejections still appear in the snackbar; the self-signed page is untouched.

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Wording | "issuing CA" on both pages | On the leaf page the issuer is a CA, not a parent |
| Shared component | Not extracted | Would rework the Create CA page; the helpers are small |

## How to Verify

```bash
dotnet test ./src/test/
dotnet build ./src/web
```

Then walk the UAT checklist in the test report in Visual Studio.
