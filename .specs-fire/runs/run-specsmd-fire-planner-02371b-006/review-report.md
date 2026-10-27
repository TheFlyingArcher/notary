# Code Review Report

**Run**: run-specsmd-fire-planner-02371b-006
**Intent**: mixed-chain-key-strength
**Files Reviewed**: 6

## Files Reviewed

- `src/library/service/KeySpecs.cs` (created)
- `src/library/service/CertificateService.cs` (modified)
- `src/test/LeafKeyStrengthTest.cs` (created)
- `src/test/InMemoryFakes.cs` (modified)
- `src/test/SignatureAlgorithmTest.cs` (modified)

## Findings

- No auto-fixes needed; `dotnet format` is clean for all four code files.
- The issuer is judged by its real key pair (curve size or modulus size), not by stored metadata, so certificates without a CA record and legacy keys with missing metadata are handled. Fail-closed: an unsupported issuer key or curve returns `Invalid`.
- A missing issuing key now returns `NotFound` instead of a null reference later.
- The check also runs for CA certificates (they go through `IssueCertificateAsync`); the message matches the CA service's, so the two paths agree.
- A #46 test paired an RSA-2048 leaf with a P-384 CA. The rule now correctly rejects that, so the test uses RSA-3072 under P-256, which still shows the issuer's key (not the subject's) picks the signature algorithm.
- Behavior to watch in UAT: the Create Certificate page still lets a user pick a leaf key below the CA's strength and will show the server error; filtering those options is the leaf UI work item.

**Tests Status**: Passing (539/539)
