---
run: run-specsmd-fire-planner-02371b-001
work_item: signature-algorithm-selector
intent: match-ecdsa-hash-to-issuer-curve
generated: 2026-10-24T00:00:00Z
status: passed
---

# Test Report: Issuer signature algorithm selector

## Summary

| Result | Count |
|--------|-------|
| Passed | 396 |
| Failed | 0 |
| Skipped | 0 |

No tests were added in this item. The helper is not yet called by any signing path, and its mapping tests are the scope of `signature-algorithm-tests`.

## Acceptance Criteria Validation

- ✅ EC mapping by curve field size (256/384/521, other falls back to SHA-256), by inspection of the switch; unit tests follow in `signature-algorithm-tests`
- ✅ RSA mapping by modulus bit length (<=2048 / <=3072 / larger), by inspection; tests follow
- ✅ Input is the issuer private key; the subject algorithm is never consulted
- ✅ An unsupported key type (or null) throws `NotaryException` naming the type
- ✅ `dotnet build` passes with 0 errors; `dotnet format --verify-no-changes` clean for the new file

## Test Commands

```bash
dotnet test ./src/test/
```
