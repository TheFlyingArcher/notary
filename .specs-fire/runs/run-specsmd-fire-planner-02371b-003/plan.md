---
run: run-specsmd-fire-planner-02371b-003
work_item: signature-algorithm-tests
intent: match-ecdsa-hash-to-issuer-curve
mode: confirm
checkpoint: plan
approved_at: 2026-10-24T00:00:00Z
---

# Implementation Plan: Signature algorithm tests

## Approach

Two layers of tests, in one new fixture plus a small fake change.

1. **Helper mapping** (fast, no key generation for RSA): `SignatureAlgorithms.ForIssuerKey` for EC keys P-256/P-384/P-521, a P-224 key to prove the SHA-256 fallback, RSA keys of 2048, 2560, 3072 and 4096 bits (built with a synthetic modulus of that bit length, since the helper reads only the size), and an unsupported key type plus null, both expecting `NotaryException`.
2. **End to end** over `CertificateService` and `CertificateRevokeService` with in-memory fakes: issue a root CA and a leaf, then assert the signature algorithm OID on the issued certificates and on the CRL, and that each verifies against the issuer's public key.
   - Issuers: P-256, P-384, P-521, RSA 2048 and RSA 3072 (one real 3072-bit generation keeps the run short).
   - Mixed pairings: an RSA leaf under a P-384 CA gets ecdsa-with-SHA384; a P-256 leaf under an RSA 2048 CA gets sha256WithRSA.

`FakeKeyService.SaveAsync` always generates a P-256 key today, ignoring the request. I will make it honor `KeyAlgorithm`, `KeyCurve` and `KeyLength` (defaulting to P-256 for EC and 2048 for RSA), so the end-to-end tests can issue under other curves. Existing tests request P-256 EC, so they should be unaffected; the full suite confirms it.

## Files to Create

| File | Purpose |
|------|---------|
| `src/test/SignatureAlgorithmTest.cs` | Helper mapping tests and end-to-end certificate/CRL signature OID tests |

## Files to Modify

| File | Changes |
|------|---------|
| `src/test/InMemoryFakes.cs` | `FakeKeyService.SaveAsync` generates the key the entity asks for |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/SignatureAlgorithmTest.cs` | EC curve and RSA size mapping, fallback, unsupported key; cert and CRL OIDs per issuer; mixed pairings; signatures verify |

## Technical Details

- NUnit 4 with `[TestCase]`/`[TestCaseSource]`, test names describing behavior, keys generated in-test, none committed.
- Uses the existing `InternalsVisibleTo` for the `internal` helper.
- No production code changes in this item.

---
*Plan approved at checkpoint. Execution follows.*
