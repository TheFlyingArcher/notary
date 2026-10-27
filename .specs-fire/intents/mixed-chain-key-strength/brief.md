---
id: mixed-chain-key-strength
title: Mixed RSA/EC chains with a no-weaker-than-issuer key strength rule
status: completed
created: 2026-10-26T00:00:00Z
completed_at: 2026-10-27T01:08:47.780Z
---

# Intent: Mixed RSA/EC chains with a no-weaker-than-issuer key strength rule

## Goal

Let a child CA choose its own key algorithm, curve and length instead of copying its parent's, and reject any new CA or leaf whose key is weaker than its issuer's. Source: GitHub issue #58.

## Users

Anyone building a Notary chain, such as an RSA root over EC intermediates, and anyone who relies on the root's security level as a guaranteed floor.

## Problem

`CertificateAuthorityService.CreateAsync` forces a child CA to copy its parent's key algorithm, curve and length, so mixed chains (valid X.509, common in public PKI) cannot be built. Once key types can differ, nothing stops a weak key from sitting under a strong CA.

## Success Criteria

- A child CA chooses its own key algorithm, curve and length
- One key-strength comparison across RSA and EC using NIST SP 800-57 security bits: P-256 = 128, P-384 = 192, P-521 = 256; RSA below 2048 = 80, 2048-3071 = 112, 3072-7679 = 128, 7680-15359 = 192, 15360 and above = 256 (off-table sizes take the row below)
- A new CA or leaf is rejected with `Result.Fail` when its security bits are lower than its direct issuer's; equal bits are allowed across types (RSA-3072 and P-256 may issue to each other)
- The rule is enforced in `CertificateAuthorityService.CreateAsync` and `CertificateService.IssueCertificateAsync`, next to the existing `CaProfileCatalog` checks
- In the UI a child's key settings default to its parent's and only options that meet the floor are offered
- Tests cover each issuer/subject key-type combination, the equal-strength boundary, and rejection below the floor

## Constraints

- New issuance only: existing chains that already break the rule keep loading and validating
- Compare against the direct issuer only; a legacy violation higher up does not block new issuance
- Signature algorithm selection and the hash-to-curve pairing were done in #46 (merged) and are out of scope
- Follow existing style (`Result<T>`, Allman braces, NUnit 4 + Moq in `src/test/`)

## Notes

The issue says this intent is tracked in the FIRE memory bank, but no `mixed-chain-key-strength` entry existed on `main` when planning started, so it was captured fresh here.
