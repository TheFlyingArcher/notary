---
run: run-specsmd-fire-planner-02371b-009
work_item: leaf-key-settings-ui
intent: mixed-chain-key-strength
generated: 2026-10-26T00:00:00Z
status: passed
---

# Test Report: Leaf key settings UI

## Summary

| Result | Count |
|--------|-------|
| Passed | 562 |
| Failed | 0 |
| Skipped | 0 |

No tests were added: the filtering, relation and wording logic is the `KeyOptions` code already unit tested for the Create CA page, and only its expected strings were updated. The razor page has no automated test project, so it is covered by `dotnet build` (0 errors) and needs UAT in Visual Studio.

## Acceptance Criteria Validation

- ✅ Key algorithm, size and curve options below the issuing CA's strength are not offered (same `KeyOptions` filtering as the CA page)
- ✅ The defaults are valid for the selected CA: `ApplyCa` still starts from the CA's own algorithm, size or curve
- ✅ Changing the key type snaps the size or curve to the weakest allowed option and refreshes the usages; the CA is chosen before the page opens, so there is no CA change on this page
- ✅ A server-side rejection is shown through the existing snackbar
- ✅ Self-signed certificates keep all options (the self-signed page is unchanged)
- ✅ `dotnet build` passes; `dotnet test ./src/test/` passes (562/562); `dotnet format` clean for the changed C# files

## UAT Checklist (Visual Studio)

1. Under an RSA 2048 CA: Create Certificate offers RSA 2048 / 3072 / 4096 and every curve, with RSA 2048 "Same as issuing CA".
2. Under a P-256 CA: RSA offers 3072 and 4096 ("Same security level"); P-256 "Same as issuing CA"; P-384 and P-521 "Stronger".
3. Under a P-384 CA: only Elliptic Curve with P-384 and P-521.
4. Under an RSA 4096 CA: RSA 3072 reads "Same security level" and the note explains the 128-bit level.
5. Switch key type under a purpose CA: the key usages follow (an EC key offers no encipherment usages).
6. Issue a certificate with a different key type than the CA (EC under an RSA CA, RSA under a stronger-or-equal EC CA) and open its details.
7. Create Certificate still shows "Issuing CA key: ..." wording; Create CA now also says "issuing CA".

## Test Commands

```bash
dotnet test ./src/test/ --filter "FullyQualifiedName~KeyOptionsTest"
```
