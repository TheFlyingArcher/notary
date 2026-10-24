---
id: match-ecdsa-hash-to-issuer-curve
title: Match signature hash to the issuer key
status: completed
created: 2026-10-23T00:00:00Z
completed_at: 2026-10-24T23:05:12.336Z
---

# Intent: Match signature hash to the issuer key

## Goal

Choose the signature algorithm from the issuer's signing key, through one shared helper used by certificate and CRL signing, so the digest matches the key's strength. Source: GitHub issue #46.

## Users

Anyone running the Notary CA, and anyone validating its certificates and CRLs, including strict linters and policy checks.

## Problem

`CertificateService.GenerateCertificate` and `CertificateRevokeService` always sign EC keys with `SHA256WithECDSA`, so P-384 and P-521 CAs sign with a digest weaker than the curve. NIST guidance, the CA/Browser Forum Baseline Requirements and CNSA 1.0 pair P-256/SHA-256, P-384/SHA-384 and P-521/SHA-512.

`GenerateCertificate` also selects the algorithm from the subject's key algorithm but signs with the issuer's key, so an RSA leaf under an EC CA (or the reverse) gets the wrong algorithm. The CA UI hides this by locking a child to its parent's algorithm.

## Success Criteria

- One shared helper picks the signature algorithm from the issuer private key; certificate and CRL signing both use it
- EC by curve field size: P-256 -> SHA256WithECDSA, P-384 -> SHA384WithECDSA, P-521 -> SHA512WithECDSA; any other curve falls back to SHA-256
- RSA scales with key size: <=2048 -> SHA256WithRSA, 3072 -> SHA384WithRSA, >=4096 -> SHA512WithRSA
- Mixed pairings (RSA leaf under EC CA and the reverse) are signed with the issuer's algorithm
- Tests assert the signature algorithm OID matches the issuer key for P-256, P-384, P-521 and each RSA size band, for certificates and CRLs, plus the mixed pairings

## Constraints

- New signatures only: existing certificates are immutable and CRLs pick up the new hash at their next regeneration
- The existing `crl.Verify` after signing stays
- Follow existing style (Allman braces, NUnit 4 + Moq in `src/test/`); no schema or API change

## Notes

Out of scope: regenerating existing CRLs at startup, new curves, changing the CA UI's algorithm lock. Only P-256, P-384 and P-521 are supported today, so the SHA-256 fallback is a safety net.
