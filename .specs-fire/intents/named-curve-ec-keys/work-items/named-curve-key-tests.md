---
id: named-curve-key-tests
title: Tests for named-curve encoding
intent: named-curve-ec-keys
complexity: medium
mode: confirm
status: completed
depends_on:
  - named-curve-key-encoding
created: 2026-10-24T00:00:00Z
run_id: run-github-issue-51-4dca63-002
completed_at: 2026-10-24T23:44:32.817Z
---

# Work Item: Tests for named-curve encoding

## Description

Prove EC keys and the certificates issued from them use named-curve encoding, including keys stored with explicit parameters.

## Acceptance Criteria

- [ ] For P-256, P-384 and P-521, a generated key's SubjectPublicKeyInfo has a named-curve OID (not explicit parameters)
- [ ] A stored key written with explicit parameters loads with the named curve, with the same public point
- [ ] Explicit parameters matching no supported curve are rejected with the unsupported-curve error
- [ ] A certificate issued from a loaded key has a named-curve SubjectPublicKeyInfo
- [ ] Existing sign/verify, `GetPublicKey` and key-migration tests still pass; whole suite and coverage threshold pass

## Technical Notes

Tests in `src/test/AsymmetricKeyServiceTest.cs` (NUnit 4 + Moq, in-memory fakes in `InMemoryFakes.cs`). Build an explicit-parameter PKCS#8 fixture by creating the key with plain `ECDomainParameters`, which is the old behaviour. Inspect the SPKI with `SubjectPublicKeyInfo.Algorithm.Parameters` (an `DerObjectIdentifier` for named, a sequence for explicit).

## Dependencies

- named-curve-key-encoding
