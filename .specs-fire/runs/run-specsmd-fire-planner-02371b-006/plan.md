---
run: run-specsmd-fire-planner-02371b-006
work_item: leaf-key-strength-floor
intent: mixed-chain-key-strength
mode: confirm
checkpoint: plan
approved_at: 2026-10-26T00:00:00Z
---

# Implementation Plan: Leaf key strength floor

## Approach

In `CertificateService.IssueCertificateAsync`, right after the issuing certificate's key pair is loaded (and before the new key is generated or anything saved), compare the requested key with the issuer's key using `KeyStrength.CheckNotWeaker` and return its failure unchanged.

- **Issuer side comes from the real key pair**, not stored metadata. A small internal helper `KeySpecs.FromKey(AsymmetricKeyParameter)` (service project) reads an EC key's field size (256, 384 or 521 maps to P-256, P-384 or P-521) or an RSA key's modulus size. An unsupported key or an unsupported curve returns `Invalid`. This also covers issuing certificates that have no CA record, and legacy keys whose stored length or curve is missing.
- **Subject side comes from the request** (`KeyAlgorithm`, `Curve`, `KeySize`). A request with no curve (EC) or no size (RSA) fails as `Invalid` through the model, before any key is made.
- If the issuing certificate's key cannot be loaded, return `NotFound` ("The issuing certificate's key was not found") instead of a null reference further down.
- The check runs for every certificate with a parent, so a CA certificate is checked here too (the same message as the CA service in the previous item). Self-signed certificates (no parent) are untouched.
- Compared against the direct issuer only.

## Files to Create

| File | Purpose |
|------|---------|
| `src/library/service/KeySpecs.cs` | Internal helper that describes a BouncyCastle key as a `KeySpec` |
| `src/test/LeafKeyStrengthTest.cs` | Issuance tests for the floor, with real keys and in-memory fakes |

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/service/CertificateService.cs` | Add the strength check after the issuer key is loaded |
| `src/test/InMemoryFakes.cs` | `FakeKeyService.Count`, so a test can prove no key was generated on rejection |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/LeafKeyStrengthTest.cs` | P-256 leaf under a P-384 CA rejected; RSA-2048 leaf under a P-256 CA rejected; P-256 leaf under RSA-2048 accepted; equal strength across types (RSA-3072 leaf under a P-256 CA) accepted; self-signed unaffected; a rejected leaf generates no key and saves nothing; helper mapping for EC curves and RSA sizes |

## Technical Details

- The full issuer/subject matrix and legacy-chain tests are the last work item; this one covers the floor itself.
- Signing already follows the issuer key (#46), so an RSA leaf under an EC CA signs correctly.
- Reuses `FakeKeyService`, which now generates the requested algorithm, curve and size.

---
*Plan approved at checkpoint. Execution follows.*
