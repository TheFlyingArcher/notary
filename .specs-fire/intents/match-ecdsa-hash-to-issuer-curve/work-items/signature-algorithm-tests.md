---
id: signature-algorithm-tests
title: Signature algorithm tests
intent: match-ecdsa-hash-to-issuer-curve
complexity: medium
mode: confirm
status: completed
depends_on:
  - use-selector-in-signing
created: 2026-10-23T00:00:00Z
run_id: run-specsmd-fire-planner-02371b-003
completed_at: 2026-10-24T23:05:12.322Z
---

# Work Item: Signature algorithm tests

## Description

Prove the signature algorithm matches the issuer key for issued certificates and generated CRLs.

## Acceptance Criteria

- [ ] Helper unit tests cover the EC mapping (P-256, P-384, P-521), the SHA-256 fallback, the RSA size bands, and rejection of an unsupported key type
- [ ] Issued certificates carry the expected signature algorithm OID for P-256, P-384, P-521 and each RSA size band
- [ ] Generated CRLs carry the expected signature algorithm OID for the same issuer keys
- [ ] Mixed pairings (RSA leaf under EC CA, EC leaf under RSA CA) use the issuer's algorithm
- [ ] `dotnet test ./src/test/` passes

## Technical Notes

Extend `CertificateServiceTest` and `CertificateRevokeServiceTest`, reusing their setup and `InMemoryFakes`; put helper tests in a new NUnit 4 fixture in `src/test/`. Never commit real keys. Prefer a small set of RSA sizes that keeps test time reasonable.

## Dependencies

- use-selector-in-signing
