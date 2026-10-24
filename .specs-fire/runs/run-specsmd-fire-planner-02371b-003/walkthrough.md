---
run: run-specsmd-fire-planner-02371b-003
work_item: signature-algorithm-tests
intent: match-ecdsa-hash-to-issuer-curve
generated: 2026-10-24T00:00:00Z
mode: confirm
---

# Implementation Walkthrough: Signature algorithm tests

## Summary

Added tests proving the signature algorithm follows the issuer key for certificates and CRLs, completing the intent. The mapping helper is covered directly, and issuance is covered end to end for each curve, two RSA sizes, and both mixed pairings.

## Files Changed

### Created

| File | Purpose |
|------|---------|
| `src/test/SignatureAlgorithmTest.cs` | Helper mapping tests and end-to-end certificate/CRL OID tests |

### Modified

| File | Changes |
|------|---------|
| `src/test/InMemoryFakes.cs` | `FakeKeyService` generates the requested algorithm, curve and size instead of always P-256 |

## Key Implementation Details

1. Mapping tests use `[TestCase]` over curves and RSA sizes either side of each band boundary (2048/2049, 3072/3073).
2. End-to-end tests issue a self-signed root and a leaf, revoke the leaf, and assert `SigAlgOid` on both certificates and the CRL, plus that each verifies against the root's public key.
3. Mixed-pairing tests prove an RSA leaf under a P-384 CA and a P-521 leaf under an RSA CA follow the issuer.

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| RSA band tests | Synthetic modulus | The helper reads only the size; avoids slow 3072/4096-bit generation |
| End-to-end RSA sizes | 2048 and 3072 | Covers SHA-256 and SHA-384; one real 3072-bit key keeps the run short |
| Fake key service | Honor the request | Needed to issue under P-384, P-521 and RSA |

## How to Verify

```bash
dotnet test ./src/test/ --filter "FullyQualifiedName~SignatureAlgorithmTest"
```

All 414 tests pass.
