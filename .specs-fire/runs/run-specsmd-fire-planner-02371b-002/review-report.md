# Code Review Report

**Run**: run-specsmd-fire-planner-02371b-002
**Intent**: match-ecdsa-hash-to-issuer-curve
**Files Reviewed**: 2

## Files Reviewed

- `src/library/service/CertificateService.cs` (modified)
- `src/library/service/CertificateRevokeService.cs` (modified)

## Findings

- No auto-fixes needed. `dotnet format` reports whitespace issues in `CertificateRevokeService.cs` from line 387 onward; they are outside the lines this run changed (342, 150, 419, 426) and were left alone.
- Behavior: the old code left `signatureFactory` null for an unrecognized `alg`; the new code can no longer produce a null factory, and an unsupported issuer key now throws `NotaryException` with a clear message.
- Security: a self-signed root passes its own key as the issuer, so roots follow the same rule. P-384/P-521 and RSA over 2048 bits now use a stronger digest; nothing weaker than before.
- `keyInfo` in `CertificateRevokeService` is still used for its null check, so it was left in place.

**Tests Status**: Passing (396/396)
