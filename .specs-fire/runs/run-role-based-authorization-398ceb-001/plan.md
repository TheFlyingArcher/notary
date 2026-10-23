---
run: run-role-based-authorization-398ceb-001
work_item: ec-key-load-validation
intent: clean-up-ec-key-loading
mode: confirm
checkpoint: plan
approved_at: 2026-10-23T01:10:00Z
---

# Implementation Plan: Simplify EC key loading and validate the public point

## Approach

Replace `LoadEcKeyPair` + `DeriveEcPublicKey` (two nested try/catch layers that log and re-wrap) with a single `LoadEcKeyPair(AsymmetricKeyParameter)`:

1. Reject anything that is not `ECPrivateKeyParameters`, naming the actual type.
2. Check the private scalar `d` is in `[1, n-1]`.
3. Derive `Q = (d*G).Normalize()`.
4. Call `ValidateEcPublicPoint(Q, domain)` (new `internal static`, reachable from `Notary.Test` via the existing `InternalsVisibleTo`): rejects infinity, a point failing `IsValid()`, and a point where `n*Q` is not infinity.
5. Build `ECPublicKeyParameters` and return the pair.

Errors are `InvalidOperationException` with specific messages ("not an EC private key: X", "private scalar out of range", "public point is not valid for the curve"). No try/catch wrapper and no logging of exception text beyond the message, so key material is never logged; wrong-passphrase handling stays in `ReadEncryptedKey`, which already has its own message.

Note: `new ECPublicKeyParameters(...)` validates internally and throws `ArgumentException`, so the explicit helper runs first on the raw point and produces our clearer message; the helper takes `(ECPoint, ECDomainParameters)` so tests can feed it bad points.

## Files to Create

(none)

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/service/AsymmetricKeyService.cs` | Collapse `LoadEcKeyPair`/`DeriveEcPublicKey`; add `ValidateEcPublicPoint` |

## Tests

Test additions belong to work item `ec-key-load-tests`. For this item I'll run the existing suite (`dotnet test ./src/test/`) to prove valid P-256/384/521 keys still load, and a quick throwaway check of the helper.

## Technical Details

Code stays Allman-braced and matches the surrounding file. Existing behavior for valid keys is unchanged.
