---
run: run-specsmd-fire-planner-02371b-008
work_item: key-strength-ux-clarity
intent: mixed-chain-key-strength
generated: 2026-10-26T00:00:00Z
status: passed
---

# Test Report: Explain equal security levels in the key pickers

## Summary

| Result | Count |
|--------|-------|
| Passed | 562 |
| Failed | 0 |
| Skipped | 0 |

15 tests were added to `KeyOptionsTest` (23 in the fixture). The razor page has no automated test project, so it is covered by `dotnet build` (0 errors) and needs UAT in Visual Studio.

## Acceptance Criteria Validation

- ✅ Each offered key option shows its security level: "RSA 3072 bit - 128-bit security", "P-384 - 192-bit security" (unit tested for every size and curve)
- ✅ Under a parent each option carries a neutral label, "Same as parent", "Same security level" or "Stronger"; the label text is tested to contain no "weak", "smaller" or "downgrade"; the chips are outlined and default-colored with no icon
- ✅ The parent's key and level are shown above the radios ("Parent CA key: RSA 4096 bit - 128-bit security")
- ✅ The note explains that keys are compared by security level, not raw size, and gives the RSA 3072 / RSA 4096 example at the 128-bit level (and only there)
- ✅ Wording lives in `core` (`KeyOptions`) for the leaf page to reuse
- ✅ RSA-4096 parent: 3072 is "Same security level", 4096 is "Same as parent"; P-256 parent: RSA-3072 same level, P-384 stronger; P-384 parent: P-521 stronger; weaker or unreadable is null; every offered option has a label
- ✅ No change to the rule or to which options are offered (`MeetsFloor`, `CheckNotWeaker` and the server are untouched)
- ✅ `dotnet build` and `dotnet test ./src/test/` pass

## UAT Checklist (Visual Studio)

1. Create an RSA 4096 root, then an intermediate under it: the key section opens with "Parent CA key: RSA 4096 bit - 128-bit security", RSA 3072 shows "Same security level", RSA 4096 shows "Same as parent", P-256 shows "Same security level", P-384 and P-521 show "Stronger".
2. The note explains security levels and mentions RSA 3072 and RSA 4096.
3. Under a P-384 parent: only P-384 ("Same as parent") and P-521 ("Stronger"); the RSA 3072 / 4096 example is not shown.
4. Create Root CA: radios show their security level, with no chips and no note.

## Test Commands

```bash
dotnet test ./src/test/ --filter "FullyQualifiedName~KeyOptionsTest"
```
