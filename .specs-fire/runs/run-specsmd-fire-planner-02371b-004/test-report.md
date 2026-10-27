---
run: run-specsmd-fire-planner-02371b-004
work_item: key-strength-model
intent: mixed-chain-key-strength
generated: 2026-10-26T00:00:00Z
status: passed
---

# Test Report: Key strength model

## Summary

| Result | Count |
|--------|-------|
| Passed | 513 |
| Failed | 0 |
| Skipped | 0 |

31 tests were added in `KeyStrengthTest`; the rest of the suite still passes.

## Acceptance Criteria Validation

- ✅ EC: P-256 = 128, P-384 = 192, P-521 = 256
- ✅ RSA: below 2048 = 80, 2048-3071 = 112, 3072-7679 = 128, 7680-15359 = 192, 15360 and above = 256; every band edge is tested (1, 2047, 2048, 3071, 3072, 7679, 7680, 15359, 15360) plus off-table sizes (2560, 4096)
- ✅ `CheckNotWeaker` fails with `ResultStatus.Invalid` when the subject has fewer bits and passes when equal or higher, across key types (RSA-3072 and P-256, RSA-7680 and P-384, RSA-15360 and P-521 in both directions)
- ✅ A missing curve, missing or non-positive length, null key, and an unreadable key on either side all fail clearly
- ✅ `dotnet build` passes; `dotnet format` is clean for both new files

## Tests Written

- `src/test/KeyStrengthTest.cs` — bits table, band edges, comparisons, failure messages, `MeetsFloor`

## Test Commands

```bash
dotnet test ./src/test/ --filter "FullyQualifiedName~KeyStrengthTest"
```
