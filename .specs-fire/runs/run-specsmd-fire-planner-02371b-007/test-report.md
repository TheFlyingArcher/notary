---
run: run-specsmd-fire-planner-02371b-007
work_item: child-ca-key-settings-ui
intent: mixed-chain-key-strength
generated: 2026-10-26T00:00:00Z
status: passed
---

# Test Report: Child CA key settings UI

## Summary

| Result | Count |
|--------|-------|
| Passed | 547 |
| Failed | 0 |
| Skipped | 0 |

8 tests were added in `KeyOptionsTest`. The razor page has no automated test project, so the page itself is covered by `dotnet build` (0 errors) and needs UAT in Visual Studio.

## Acceptance Criteria Validation

- ✅ Choosing a parent still pre-fills algorithm, curve and length from the parent (existing handler, unchanged)
- ✅ Options below the parent's strength are not offered: a P-384 parent offers no RSA and only P-384/P-521; a P-521 parent offers P-521 only; a P-256 or RSA-3072 parent offers RSA 3072/4096 and every curve (unit tested)
- ✅ A user can still raise strength or switch between RSA and EC when the strength is at least the parent's (the radios are no longer disabled under a parent; switching type snaps to the weakest allowed option)
- ✅ Root CAs keep all options (a null issuer offers everything; unit tested)
- ✅ A server-side rejection is shown through the existing snackbar
- ✅ `dotnet build` passes for the web project; `dotnet format` clean for the changed C# files

## Tests Written

- `src/test/KeyOptionsTest.cs` — root, P-256, P-384, P-521, RSA-2048, RSA-3072 and unreadable issuers; the description text

## UAT Checklist (Visual Studio)

1. Create Root CA with RSA-2048; open Create Intermediate under it: all key options are offered and the parent's key is selected.
2. Under a P-256 root: the RSA option offers 3072 and 4096 only; switching to RSA selects 3072.
3. Under a P-384 root: only Elliptic Curve is offered, with P384 and P521.
4. Under a P-521 root: only Elliptic Curve with P521.
5. Create a mixed chain (RSA-3072 root, EC P-256 intermediate) and confirm it is created.
6. The Create Root CA page still shows every option with no note.

## Test Commands

```bash
dotnet test ./src/test/ --filter "FullyQualifiedName~KeyOptionsTest"
```
