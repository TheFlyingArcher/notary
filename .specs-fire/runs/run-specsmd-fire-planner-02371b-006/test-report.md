---
run: run-specsmd-fire-planner-02371b-006
work_item: leaf-key-strength-floor
intent: mixed-chain-key-strength
generated: 2026-10-26T00:00:00Z
status: passed
---

# Test Report: Leaf key strength floor

## Summary

| Result | Count |
|--------|-------|
| Passed | 539 |
| Failed | 0 |
| Skipped | 0 |

15 test cases were added in `LeafKeyStrengthTest`. One #46 test (`RsaLeafUnderEcCa_UsesTheEcIssuersAlgorithm`) was changed because the rule now correctly rejects its old pairing.

## Acceptance Criteria Validation

- ✅ A leaf weaker than its issuing CA is rejected with `ResultStatus.Invalid` before any key is generated or saved (P-256 under P-384, P-384 under P-521, RSA-2048 under P-256); the key count and the saved certificates are unchanged
- ✅ A leaf at equal or higher strength is accepted: P-256 under RSA-2048, RSA-3072 under P-256 (equal), P-384 under P-384
- ✅ Self-signed certificates have no issuer and are unaffected
- ✅ A CA certificate weaker than its issuer is rejected through the same check
- ✅ The check sits in `IssueCertificateAsync` next to the `CaProfileCatalog.CheckLeaf` call
- ✅ An EC leaf with no curve is rejected as `Invalid`
- ✅ `KeySpecs` reads P-256/P-384/P-521 and the RSA modulus size from real keys, and rejects other key types
- ✅ `dotnet test ./src/test/` passes (539/539)

## Tests Written

- `src/test/LeafKeyStrengthTest.cs` — floor, boundaries, no side effects on rejection, `KeySpecs` mapping

## Test Commands

```bash
dotnet test ./src/test/ --filter "FullyQualifiedName~LeafKeyStrengthTest"
```

## Notes

The full issuer/subject matrix and legacy-chain tests belong to `key-strength-tests-docs`.
