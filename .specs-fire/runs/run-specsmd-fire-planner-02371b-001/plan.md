---
run: run-specsmd-fire-planner-02371b-001
work_item: signature-algorithm-selector
intent: match-ecdsa-hash-to-issuer-curve
mode: confirm
checkpoint: plan
approved_at: 2026-10-24T00:00:00Z
---

# Implementation Plan: Issuer signature algorithm selector

## Approach

Add an `internal static class SignatureAlgorithms` in `src/library/service/` with one method, `ForIssuerKey(AsymmetricKeyParameter issuerPrivateKey)`, returning the BouncyCastle algorithm name string accepted by `Asn1SignatureFactory`.

- `ECPrivateKeyParameters`: switch on `Parameters.Curve.FieldSize` (256 -> `SHA256WithECDSA`, 384 -> `SHA384WithECDSA`, 521 -> `SHA512WithECDSA`, otherwise `SHA256WithECDSA`).
- `RsaKeyParameters` (covers `RsaPrivateCrtKeyParameters`): switch on `Modulus.BitLength` (<=2048 -> `SHA256WithRSA`, <=3072 -> `SHA384WithRSA`, otherwise `SHA512WithRSA`).
- Anything else (including a public key) throws `NotaryException` naming the unsupported type, since reaching it means a programming error rather than a business failure.

The helper is not wired into signing here; that is the next work item. Tests for the mapping arrive in `signature-algorithm-tests`, so this item only builds.

## Files to Create

| File | Purpose |
|------|---------|
| `src/library/service/SignatureAlgorithms.cs` | Shared issuer-key to signature-algorithm mapping |

## Files to Modify

| File | Changes |
|------|---------|
| (none) | |

## Technical Details

- `internal` is reachable from `Notary.Test` via the existing `InternalsVisibleTo`.
- RSA bands: the intent specifies 2048 -> SHA-256, 3072 -> SHA-384, 4096+ -> SHA-512. A modulus between bands (e.g. 2560) rounds up to the next band's hash; below 2048 stays SHA-256.
- The key's private-ness is not checked beyond the type; `Asn1SignatureFactory` already rejects public keys at signing.
- Allman bracing, file-scoped namespace `Notary.Service`.

---
*Plan approved at checkpoint. Execution follows.*
