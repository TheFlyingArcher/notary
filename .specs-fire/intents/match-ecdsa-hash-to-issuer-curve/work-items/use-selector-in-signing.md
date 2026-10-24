---
id: use-selector-in-signing
title: Use the selector for certificate and CRL signing
intent: match-ecdsa-hash-to-issuer-curve
complexity: medium
mode: confirm
status: completed
depends_on:
  - signature-algorithm-selector
created: 2026-10-23T00:00:00Z
run_id: run-specsmd-fire-planner-02371b-002
completed_at: 2026-10-24T22:59:55.035Z
---

# Work Item: Use the selector for certificate and CRL signing

## Description

Replace the subject-algorithm switch in `CertificateService.GenerateCertificate` (~line 428) and the inline choice in `CertificateRevokeService` (~line 342) with the shared helper, keyed off the issuer private key.

## Acceptance Criteria

- [ ] `GenerateCertificate` signs with the algorithm the helper returns for `issuerKeyPair.Private`
- [ ] CRL signing uses the same helper with the CA key pair
- [ ] An RSA leaf under an EC CA, and the reverse, is signed with the issuer's algorithm
- [ ] The `alg` parameter is removed from `GenerateCertificate` if nothing else uses it
- [ ] The existing `crl.Verify` after signing is kept
- [ ] Existing tests pass: `dotnet test ./src/test/`

## Technical Notes

New signatures only; existing certificates and CRLs are unchanged. No schema or API change.

## Dependencies

- signature-algorithm-selector
