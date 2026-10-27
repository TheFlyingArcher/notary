---
run: run-specsmd-fire-planner-02371b-004
work_item: key-strength-model
intent: mixed-chain-key-strength
mode: confirm
checkpoint: plan
approved_at: 2026-10-26T00:00:00Z
---

# Implementation Plan: Key strength model

## Approach

Add a new `public static class KeyStrength` in namespace `Notary.Security` (folder `src/library/core/Security/`, beside the other cross-cutting security types). It is pure and stateless, in `core` so the services, the web pages and the tests can all use it.

- `Result<int> GetSecurityBits(Algorithm algorithm, EllipticCurve? curve, int? keyLength)`:
  - EC needs a curve: P-256 = 128, P-384 = 192, P-521 = 256. A missing curve is `Result.Fail(Invalid, ...)`.
  - RSA needs a length: below 2048 = 80, 2048-3071 = 112, 3072-7679 = 128, 7680-15359 = 192, 15360 and above = 256, so off-table sizes take the row below. A missing or non-positive length is `Fail(Invalid, ...)`.
- `Result CheckNotWeaker(issuer, subject)` takes two small positional records `KeySpec(Algorithm Algorithm, EllipticCurve? Curve, int? Length)` and fails with `ResultStatus.Invalid` and a message naming both strengths when the subject has fewer bits than the issuer. Equal or higher passes, across key types (RSA-3072 under P-256 and the reverse).
- `bool MeetsFloor(KeySpec issuer, KeySpec candidate)` for the UI items to filter options without handling a `Result`; an unreadable key on either side returns false.

Convenience: a `KeySpec` constructor path from `CertificateAuthority` and `AsymmetricKey` is left to the work items that need it, to keep this item to the model.

## Files to Create

| File | Purpose |
|------|---------|
| `src/library/core/Security/KeyStrength.cs` | NIST security-bits table and the not-weaker-than-issuer check, plus the `KeySpec` record |

## Files to Modify

| File | Changes |
|------|---------|
| (none) | |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/KeyStrengthTest.cs` | Bits for each curve and every RSA band edge (1, 2047, 2048, 3071, 3072, 7679, 7680, 15359, 15360); missing curve or length; equal, higher and lower comparisons across RSA/EC combinations; `MeetsFloor` |

## Technical Details

- Matches the contract the later items rely on: `Result`/`ResultStatus.Invalid` for rule violations, no exceptions.
- Messages state the bits (for example "a 112-bit key cannot be issued under a 128-bit CA") so the UI can show them as is.
- Allman braces, file-scoped namespace, positional record for the DTO, per the coding standards.
- Tests are added here rather than deferred, because the model has no other caller yet.

---
*Plan approved at checkpoint. Execution follows.*
