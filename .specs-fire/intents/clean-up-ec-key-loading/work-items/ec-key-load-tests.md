---
id: ec-key-load-tests
title: Tests for EC key loading and public key matching
intent: clean-up-ec-key-loading
complexity: medium
mode: confirm
status: completed
depends_on:
  - ec-key-load-validation
created: 2026-10-22T00:00:00Z
run_id: run-role-based-authorization-398ceb-002
completed_at: 2026-10-23T01:09:53.259Z
---

# Work Item: Tests for EC key loading and public key matching

## Description

Add the tests issue #47 asks for that are not yet in `AsymmetricKeyServiceTest`.

## Acceptance Criteria

- [ ] For P-256, P-384, P-521: generate, store, load; the loaded public key's SubjectPublicKeyInfo equals the SPKI of a certificate issued for that key
- [ ] The validation helper rejects a point at infinity and a point not on the curve
- [ ] Existing round-trip sign/verify tests still cover all three curves (no duplicate added)
- [ ] `dotnet test ./src/test/` passes

## Technical Notes

Extend `src/test/AsymmetricKeyServiceTest.cs`, reusing its `AllEllipticCurves` source and setup. Build the certificate in-test (self-signed with BouncyCastle); never commit real keys. If the validation helper is private, expose it `internal` with `InternalsVisibleTo` only if the project already does so; otherwise test through `GetKeyPairAsync`.

## Dependencies

- ec-key-load-validation
