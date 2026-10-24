---
run: run-specsmd-fire-planner-02371b-002
work_item: use-selector-in-signing
intent: match-ecdsa-hash-to-issuer-curve
mode: confirm
checkpoint: plan
approved_at: 2026-10-24T00:00:00Z
---

# Implementation Plan: Use the selector for certificate and CRL signing

## Approach

Route both signing paths through `SignatureAlgorithms.ForIssuerKey` so the issuer private key alone decides the algorithm.

- `CertificateService.GenerateCertificate`: drop the `switch (alg)` and build `new Asn1SignatureFactory(SignatureAlgorithms.ForIssuerKey(issuerKeyPair.Private), issuerKeyPair.Private)`. Remove the now-unused `Algorithm alg` parameter and the `request.KeyAlgorithm` argument at the single call site (line 148). The subject's algorithm still drives subject key generation.
- `CertificateRevokeService`: replace the `keyInfo.KeyAlgorithm` ternary with `SignatureAlgorithms.ForIssuerKey(keyPair.Private)`. `keyInfo` is still used for its null check, so that stays.
- The existing `crl.Verify` after signing is kept.

Behavior change for valid inputs: P-384 and P-521 issuers and RSA issuers over 2048 bits now sign with SHA-384/512. P-256 and RSA 2048 issuers are unchanged. An RSA leaf under an EC CA (or the reverse) now uses the issuer's algorithm instead of the subject's. Existing certificates and CRLs are untouched.

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/service/CertificateService.cs` | Use the selector in `GenerateCertificate`; remove the `alg` parameter and the switch |
| `src/library/service/CertificateRevokeService.cs` | Use the selector for CRL signing |

## Tests

| Test File | Coverage |
|-----------|----------|
| (none new) | Existing suite must still pass; new assertions arrive in `signature-algorithm-tests` |

## Technical Details

- If `KeyService.GetKeyPairAsync` returned null for the issuer, signing already failed before this change; no new null handling is added.
- Only RSA 2048 and the three EC curves are exercised by existing tests, so passing tests confirm no regression for the unchanged bands.

---
*Plan approved at checkpoint. Execution follows.*
