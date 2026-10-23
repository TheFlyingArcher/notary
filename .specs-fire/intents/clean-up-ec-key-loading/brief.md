---
id: clean-up-ec-key-loading
title: Clean up and test EC key loading
status: completed
created: 2026-10-22T00:00:00Z
completed_at: 2026-10-23T01:09:53.271Z
---

# Intent: Clean up and test EC key loading

## Goal

Make EC key loading in `AsymmetricKeyService` simple and explicitly safe, and cover it with tests, so the next EC change (named domain parameters, #43) has a solid base. Source: GitHub issue #47.

## Users

Maintainers of Notary. Nothing here is user-visible.

## Problem

Issue #47 was filed before #48/#49/#55. Those already removed the `Q` reflection and dead `try`, fixed `GetPublicKey` to return the DER SubjectPublicKeyInfo, and replaced the misleading "properly encrypted" message. Remaining gaps:

- `LoadEcKeyPair` still has a redundant try/catch layering and a double-wrapping `DeriveEcPublicKey`.
- The public point `Q = d*G` is derived but never explicitly validated against the curve, so the guarantee would silently disappear if derivation changes (e.g. with #43).
- No test proves the loaded public key matches the certificate's SubjectPublicKeyInfo, or that validation rejects bad points.

## Success Criteria

- `LoadEcKeyPair` / `DeriveEcPublicKey` collapse to one straightforward path with accurate error messages (corrupt key vs unsupported curve vs wrong passphrase)
- The derived public point is explicitly validated: normalized, not infinity, `IsValid()`, and `n*Q == infinity`
- Tests: loaded public key equals the certificate's SubjectPublicKeyInfo for P-256, P-384, P-521; validation rejects an invalid point
- Existing round-trip sign/verify, `GetPublicKey` and migration tests keep passing

## Constraints

- Follow existing style (Allman braces, file-scoped conventions in the surrounding file, NUnit 4 + Moq in `src/test/`)
- No behaviour change for valid keys; no schema or API change
- Must stay compatible with moving to named domain parameters (#43)

## Notes

Scope confirmed with the user: remaining gaps only, with explicit validation after derivation.
