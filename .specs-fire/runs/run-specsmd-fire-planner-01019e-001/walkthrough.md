---
run: run-specsmd-fire-planner-01019e-001
work_item: certificates-by-ca-listing
intent: organize-certificates-by-ca
generated: 2026-10-13T23:30:00Z
mode: confirm
---

# Implementation Walkthrough: Scoped certificate listing by CA

## Summary

Adds the three queries the Certificates page needs: self-signed certificates, certificates per issuing CA (already existed), and the list of non-revoked CAs that can issue leaf certificates. No UI changes yet.

## Key point

`Certificate.IssuingSlug` is the issuer's **certificate** slug (`CertificateAuthority.CertificateSlug`), not the CA slug. Callers of `GetCertificatesByCaAsync` must pass `ca.CertificateSlug`. Self-signed certificates have an empty issuing slug.

## Files Changed

### Created

| File | Purpose |
|------|---------|
| `src/test/CertificateListingTest.cs` | 7 tests for the three queries |

### Modified

| File | Changes |
|------|---------|
| `ICertificateRepository.cs`, `ICertificateService.cs` | `GetSelfSignedAsync` / `GetSelfSignedCertificatesAsync`; doc clarified |
| `ICertificateAuthorityService.cs`, `CertificateAuthorityService.cs` | `GetLeafIssuingAuthoritiesAsync`: issuers whose profile issues leaf certificates and whose CA certificate is not revoked, sorted by name |
| `CertificateRepository.cs` | Mongo filter: non-CA certificates with null or empty `iss_slug` |
| `CertificateService.cs` | Delegates self-signed listing to the repository |
| `InMemoryFakes.cs` | Fake `GetSelfSignedAsync` |

## How to Verify

```bash
dotnet test ./src/test/
```

The Mongo self-signed filter is exercised for real only in the UI work items.
