---
run: run-specsmd-fire-planner-02371b-010
work_item: key-strength-tests-docs
intent: mixed-chain-key-strength
generated: 2026-10-26T00:00:00Z
status: passed
---

# Test Report: Key strength tests and docs

## Summary

| Result | Count |
|--------|-------|
| Passed | 578 |
| Failed | 0 |
| Skipped | 0 |

16 tests were added: 15 in `MixedChainTest` (2 chains and 13 type-matrix cases) and 1 in `CertificateAuthorityServiceTest`.

## Acceptance Criteria Validation

- ✅ CA and leaf issuance across RSA→RSA, RSA→EC, EC→RSA and EC→EC, each accepted at equal or higher strength and rejected below (13 cases, run through the real `CertificateAuthorityService` and `CertificateService`)
- ✅ Equal-strength boundaries: RSA-3072 with P-256 in both directions is accepted; RSA-2048 under RSA-3072, P-256 under P-384, P-521 over P-384 are rejected. RSA-3072 against RSA-4096 sits in the existing model tests (no 4096-bit key is generated)
- ✅ Real mixed chains verify: an RSA-3072 root with an EC P-256 intermediate and RSA/EC leaves, and an EC P-384 chain with a P-521 leaf; every certificate verifies against its issuer's public key and is signed with the issuer's algorithm
- ✅ Rejected requests leave nothing behind: no CA record, certificate or key is created
- ✅ A CA already below its own issuer still loads, and a child under it is compared only with that direct parent (the stronger grandparent is never consulted)
- ✅ Docs: `docs/key-strength.md` describes mixed chains, the rule, the table, RSA-3072 vs RSA-4096, where it is enforced, signatures following the issuer, the pages, and existing chains; the README links to it
- ✅ `dotnet test ./src/test/` passes (578/578); `dotnet format` clean for the test files

## Tests Written

- `src/test/MixedChainTest.cs` — chains and issuer/subject matrix
- `src/test/CertificateAuthorityServiceTest.cs` — direct-issuer-only comparison

## Test Commands

```bash
dotnet test ./src/test/ --filter "FullyQualifiedName~MixedChainTest"
```
