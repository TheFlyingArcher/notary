---
run: run-github-issue-51-4dca63-002
work_item: named-curve-key-encoding
intent: named-curve-ec-keys
mode: confirm
checkpoint: plan
approved_at: 2026-10-24T23:50:00Z
---

# Implementation Plan: Named-curve EC key generation and loading

## Approach

- `GenerateKeyPair`: initialise `ECKeyGenerationParameters` with the curve OID (`GetCurveOid`) instead of a plain `ECDomainParameters`, so BouncyCastle produces `ECNamedDomainParameters`. The SubjectPublicKeyInfo and the PKCS#8 written by `WriteEncryptedKey` then carry the curve OID.
- `LoadEcKeyPair`: if the loaded key's domain is not already an `ECNamedDomainParameters` (a key stored with explicit parameters), match its curve, G, N and H against `SecNamedCurves.GetByOid` for P-256, P-384 and P-521, and rebuild the private key with `ECNamedDomainParameters`. No match throws `InvalidOperationException` ("unsupported curve", no key material). The existing range check and `ValidateEcPublicPoint` then run on the named domain.
- Stored rows are not rewritten; the private scalar and public point are unchanged.

## Files to Create

(none)

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/service/AsymmetricKeyService.cs` | Named-curve generation; explicit-to-named normalization in `LoadEcKeyPair` |
| `src/test/AsymmetricKeyServiceTest.cs` | Work item 2: SPKI named-curve for P-256/384/521, explicit-parameter stored key normalized, unknown curve rejected, certificate SPKI named-curve |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/AsymmetricKeyServiceTest.cs` | Generated and loaded key SPKI has a named-curve OID for each curve; explicit-parameter fixture loads with same public point; unsupported curve rejected; issued certificate has named-curve SPKI; existing tests unchanged |

## Technical Details

Both work items run in this one batch run; the tests item follows the implementation. Verification: `dotnet test src/Notary.sln -c Release` with the coverage threshold used in CI, plus a manual UAT: generate a P-384 root/intermediate/leaf and run `openssl verify` (no error 94). Certificates already issued keep explicit parameters and must be re-issued. #45 (key usages) is out of scope.

---
*Plan approved at checkpoint. Execution follows.*
