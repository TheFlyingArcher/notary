---
run: run-specsmd-fire-planner-02371b-002
work_item: use-selector-in-signing
intent: match-ecdsa-hash-to-issuer-curve
generated: 2026-10-24T00:00:00Z
mode: confirm
---

# Implementation Walkthrough: Use the selector for certificate and CRL signing

## Summary

Certificates and CRLs now take their signature algorithm from the issuer's private key through `SignatureAlgorithms.ForIssuerKey`. P-384 and P-521 issuers sign with SHA-384 and SHA-512, RSA issuers above 2048 bits scale up, and an RSA leaf under an EC CA (or the reverse) follows the issuer instead of the subject.

## Files Changed

### Modified

| File | Changes |
|------|---------|
| `src/library/service/CertificateService.cs` | `GenerateCertificate` builds its `Asn1SignatureFactory` from the helper; the `alg` parameter, its `switch` and the call-site argument are gone |
| `src/library/service/CertificateRevokeService.cs` | CRL signing uses the helper in place of the `keyInfo.KeyAlgorithm` ternary |

## Key Implementation Details

1. In `GenerateCertificate` the subject's algorithm now only drives subject key generation; the signature depends on `issuerKeyPair.Private`.
2. A self-signed root passes its own key pair as the issuer, so roots follow the same mapping.
3. `crl.Verify` after signing is unchanged.

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Remove `alg` parameter | Yes | Nothing else read it once the switch was gone |
| Existing artifacts | Untouched | New signatures only, per the intent |

## How to Verify

```bash
dotnet test ./src/test/
```

All 396 existing tests pass. Per-curve and per-band OID assertions come with `signature-algorithm-tests`.
