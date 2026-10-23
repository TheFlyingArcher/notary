# Walkthrough: Simplify EC key loading and validate the public point

## What changed
`AsymmetricKeyService.LoadEcKeyPair` is now a single static method: type check, private-scalar range check (1 <= d < n), derive `Q = d*G` (normalized), validate, return the pair. `DeriveEcPublicKey` and its try/catch/log wrappers are gone.

New `internal static ValidateEcPublicPoint(ECPoint, ECDomainParameters)` rejects the point at infinity, a point not on the curve, and a point where `n*Q` is not infinity. It is internal so `Notary.Test` can call it (existing `InternalsVisibleTo`) and it keeps working when #43 switches to named domain parameters.

## Verification
`dotnet test ./src/test/`: 383 passed. Direct rejection tests arrive with `ec-key-load-tests`.

## Next
`ec-key-load-tests` (medium, confirm).
