---
run: run-specsmd-fire-planner-02371b-003
work_item: signature-algorithm-tests
intent: match-ecdsa-hash-to-issuer-curve
generated: 2026-10-24T00:00:00Z
status: passed
---

# Test Report: Signature algorithm tests

## Summary

| Result | Count |
|--------|-------|
| Passed | 414 |
| Failed | 0 |
| Skipped | 0 |

18 tests added in `SignatureAlgorithmTest`; the 396 existing tests still pass after `FakeKeyService` began honoring the requested algorithm, curve and size.

## Acceptance Criteria Validation

- ✅ Helper mapping: P-256/P-384/P-521, the SHA-256 fallback for P-224, RSA at 1024/2048/2049/3072/3073/4096 bits, and `NotaryException` for an unsupported key and for null
- ✅ Issued certificates (self-signed root and leaf) carry the expected signature OID for P-256, P-384, P-521, RSA 2048 and RSA 3072, and verify against the issuer's public key
- ✅ Generated CRLs carry the same OID for the same issuers and verify
- ✅ Mixed pairings: an RSA leaf under a P-384 CA is ecdsa-with-SHA384; a P-521 leaf under an RSA 2048 CA is sha256WithRSA
- ✅ `dotnet test ./src/test/` passes (414/414)

## Tests Written

### Unit Tests

- `src/test/SignatureAlgorithmTest.cs` — `ForIssuerKey_*` mapping tests

### Integration Tests

- `src/test/SignatureAlgorithmTest.cs` — end-to-end certificate and CRL OID tests, mixed pairings

## Test Commands

```bash
dotnet test ./src/test/
```

## Notes

RSA band tests use a synthetic modulus of the target size (the helper reads only the size), so no 4096-bit key is generated. One real 3072-bit key is generated for the end-to-end case.
