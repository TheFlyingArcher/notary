---
id: signature-algorithm-selector
title: Issuer signature algorithm selector
intent: match-ecdsa-hash-to-issuer-curve
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-23T00:00:00Z
run_id: run-specsmd-fire-planner-02371b-001
completed_at: 2026-10-24T22:51:47.672Z
---

# Work Item: Issuer signature algorithm selector

## Description

Add one shared helper in `src/library/service/` that returns the BouncyCastle signature algorithm name for an issuer private key, so certificate and CRL signing choose the same way.

## Acceptance Criteria

- [ ] EC keys map by curve field size: 256 -> `SHA256WithECDSA`, 384 -> `SHA384WithECDSA`, 521 -> `SHA512WithECDSA`; any other curve falls back to `SHA256WithECDSA`
- [ ] RSA keys map by modulus size: <=2048 -> `SHA256WithRSA`, 3072 -> `SHA384WithRSA`, >=4096 -> `SHA512WithRSA`
- [ ] Input is the issuer private key (`AsymmetricKeyParameter`); the subject's algorithm is never consulted
- [ ] An unsupported key type fails clearly (no silent default)
- [ ] `dotnet build` passes

## Technical Notes

Static helper, no state. Read the EC field size from the key's domain parameters curve and the RSA size from the modulus bit length. Follow the surrounding Allman style and naming.

## Dependencies

(none)
