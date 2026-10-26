---
id: named-curve-key-encoding
title: Named-curve EC key generation and loading
intent: named-curve-ec-keys
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-24T00:00:00Z
run_id: run-github-issue-51-4dca63-002
completed_at: 2026-10-24T23:44:29.102Z
---

# Work Item: Named-curve EC key generation and loading

## Description

Make `AsymmetricKeyService` generate and load EC keys with named-curve domain parameters so the SubjectPublicKeyInfo and stored PKCS#8 form carry the curve OID.

## Acceptance Criteria

- [ ] `GenerateKeyPair` builds named domain parameters (`ECNamedDomainParameters` with the curve OID) for P-256, P-384 and P-521
- [ ] `LoadEcKeyPair` uses the named curve; a key whose PKCS#8 carries explicit parameters is matched to P-256/384/521 and loaded with the named curve
- [ ] Explicit parameters matching none of the three curves fail with an `InvalidOperationException` that says the curve is unsupported (no key material in the message)
- [ ] Newly written encrypted keys carry the curve OID in their PKCS#8 AlgorithmIdentifier
- [ ] Stored rows are not rewritten; the private scalar and public point are unchanged for existing keys

## Technical Notes

`src/library/service/AsymmetricKeyService.cs` (`GenerateKeyPair` ~line 179, `GetCurveOid`, `LoadEcKeyPair` ~line 239). BouncyCastle `PrivateKeyFactory` returns `ECNamedDomainParameters` for named-curve PKCS#8 and plain `ECDomainParameters` for explicit ones. Match explicit parameters by comparing curve, G, N and H against `SecNamedCurves.GetByOid` for the three supported OIDs. Keep `ValidateEcPublicPoint` as is.

## Dependencies

(none)
