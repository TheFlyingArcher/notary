---
id: ec-key-load-validation
title: Simplify EC key loading and validate the public point
intent: clean-up-ec-key-loading
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-22T00:00:00Z
run_id: run-role-based-authorization-398ceb-001
completed_at: 2026-10-23T01:06:45.008Z
---

# Work Item: Simplify EC key loading and validate the public point

## Description

Collapse `LoadEcKeyPair` and `DeriveEcPublicKey` in `AsymmetricKeyService` into one clear path, and explicitly validate the derived public point against the curve.

## Acceptance Criteria

- [ ] One path from `ECPrivateKeyParameters` to the key pair; no redundant catch/rethrow layers
- [ ] Non-EC input (wrong parameter type) fails with a message naming the actual type
- [ ] The derived `Q` is normalized and rejected if infinity, if `Q.IsValid()` is false, or if `n*Q` is not infinity
- [ ] Failure messages distinguish an invalid/corrupt key from a wrong passphrase; the key material is never logged
- [ ] Valid P-256, P-384 and P-521 keys load exactly as before

## Technical Notes

`src/library/service/AsymmetricKeyService.cs` (`LoadEcKeyPair`, `DeriveEcPublicKey`, ~lines 224-268). Put the validation in a small private static helper that takes `ECPublicKeyParameters`, so it can be exercised directly by tests and survives #43. `ECPublicKeyParameters` already validates on construction in recent BouncyCastle, so confirm what is redundant before adding checks.

## Dependencies

(none)
