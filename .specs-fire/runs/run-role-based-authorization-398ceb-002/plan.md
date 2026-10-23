---
run: run-role-based-authorization-398ceb-002
work_item: ec-key-load-tests
intent: clean-up-ec-key-loading
mode: confirm
checkpoint: plan
approved_at: 2026-10-23T01:20:00Z
---

# Implementation Plan: Tests for EC key loading and public key matching

## Approach

Add tests to the existing `AsymmetricKeyServiceTest` (reusing `CreateService`, `CreateKeyAsync`, `AllEllipticCurves`):

1. `LoadedPublicKey_MatchesSelfSignedCertificateSubjectPublicKeyInfo(curve)`, per curve: save and load a key, build a self-signed X.509 certificate in-test with BouncyCastle (`X509V3CertificateGenerator`, `Asn1SignatureFactory("SHA256withECDSA")`) from the loaded public key, signed with the loaded private key. Assert the certificate's SubjectPublicKeyInfo equals both the loaded pair's SPKI and `GetPublicKey(slug)`, and that the certificate verifies against its own public key.
2. `ValidateEcPublicPoint` tests (via `InternalsVisibleTo`), using the P-256 domain:
   - valid point passes (per curve, via TestCaseSource);
   - point at infinity throws `InvalidOperationException` ("infinity");
   - off-curve point (valid x, y+1, built with `curve.CreatePoint`) throws ("not on the curve");
   - null point throws.
3. A test that a stored RSA key loaded as EC (`LoadEcKeyPair` is private) is not feasible directly, so skip it; the wrong-type message is covered by review. (Existing round-trip tests already cover sign/verify for all three curves, so no duplicate is added.)

Then run `dotnet test ./src/test/`.

## Files to Create

(none)

## Files to Modify

| File | Changes |
|------|---------|
| `src/test/AsymmetricKeyServiceTest.cs` | Add the SPKI-vs-certificate test and the `ValidateEcPublicPoint` tests |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/AsymmetricKeyServiceTest.cs` | Loaded public key vs certificate SPKI (P-256/384/521); point validation accept/reject paths |

## Technical Details

Test keys are generated in-test; no keys are committed. The certificate is in-memory only.
