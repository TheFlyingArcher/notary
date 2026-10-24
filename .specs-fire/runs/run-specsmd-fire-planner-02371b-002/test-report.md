---
run: run-specsmd-fire-planner-02371b-002
work_item: use-selector-in-signing
intent: match-ecdsa-hash-to-issuer-curve
generated: 2026-10-24T00:00:00Z
status: passed
---

# Test Report: Use the selector for certificate and CRL signing

## Summary

| Result | Count |
|--------|-------|
| Passed | 396 |
| Failed | 0 |
| Skipped | 0 |

No tests were added; OID assertions for each curve, RSA band and mixed pairing are the scope of `signature-algorithm-tests`. The existing suite (issuance, CRL generation with `crl.Verify`, CA hierarchy) exercises the unchanged P-256 and RSA 2048 paths through the new code.

## Acceptance Criteria Validation

- ✅ `GenerateCertificate` signs with `SignatureAlgorithms.ForIssuerKey(issuerKeyPair.Private)`
- ✅ CRL signing uses the same helper with the CA key pair
- ✅ Mixed pairings follow the issuer's algorithm (the subject's algorithm no longer participates); asserted by tests in the next work item
- ✅ The `alg` parameter and its call-site argument are removed; nothing else used it
- ✅ `crl.Verify` after signing is kept
- ✅ `dotnet test ./src/test/` passes (396/396)

## Test Commands

```bash
dotnet test ./src/test/
```
