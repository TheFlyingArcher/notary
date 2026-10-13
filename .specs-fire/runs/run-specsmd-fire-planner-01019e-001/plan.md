---
run: run-specsmd-fire-planner-01019e-001
work_item: certificates-by-ca-listing
intent: organize-certificates-by-ca
mode: confirm
checkpoint: plan
approved_at: 2026-10-13T23:40:00Z
---

# Implementation Plan: Scoped certificate listing by CA

## Approach

Finding: `Certificate.IssuingSlug` (`iss_slug`) holds the issuer's **certificate** slug, i.e. `CertificateAuthority.CertificateSlug`, not the CA slug. The existing `GetCertificatesByCaAsync` already filters on it (despite its `caSlug` parameter name), so the UI must pass `ca.CertificateSlug`. Self-signed certificates are issued with no `ParentCertificateSlug`, so their `iss_slug` is null/empty.

1. Self-signed listing: add `GetSelfSignedAsync()` to `ICertificateRepository` (non-CA certificates with null or empty `iss_slug`) and `GetSelfSignedCertificatesAsync()` to `ICertificateService`.
2. CA dropdown source: add `GetLeafIssuingAuthoritiesAsync()` to `ICertificateAuthorityService`. It returns CAs where `IsIssuer` and `CaProfileCatalog.Resolve(ca).IssuesLeafCertificates` hold and whose CA certificate is not revoked, sorted by name. This is the same filter `CreateCertificate.razor.cs` applies today plus the revocation check.
3. Update the `GetCertificatesByCaAsync` doc comment and parameter name to say it takes the issuing CA certificate slug (no behavior change).
4. Extend `FakeCertificateRepository` and add unit tests.

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/core/Interface/Repository/ICertificateRepository.cs` | Add `GetSelfSignedAsync`; clarify slug doc |
| `src/library/core/Interface/Service/ICertificateService.cs` | Add `GetSelfSignedCertificatesAsync` |
| `src/library/core/Interface/Service/ICertificateAuthorityService.cs` | Add `GetLeafIssuingAuthoritiesAsync` |
| `src/library/data/Repository/CertificateRepository.cs` | Mongo filter for self-signed leaf certificates |
| `src/library/service/CertificateService.cs` | Delegate self-signed query to repository |
| `src/library/service/CertificateAuthorityService.cs` | Filter CAs by issuer, profile and non-revoked certificate; sort by name |
| `src/test/InMemoryFakes.cs` | Implement `GetSelfSignedAsync` in the fake repository |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/CertificateAuthorityServiceTest.cs` | Leaf-issuing CAs only (roots, non-issuers excluded), revoked CA cert excluded, name ordering, empty result |
| `src/test/CertificateServiceTest.cs` | Self-signed query returns only non-CA certs without an issuer; per-CA query returns only that issuer's certificates |

## Technical Details

Revocation check: `CertificateService.GetAsync(ca.CertificateSlug)` and `RevocationDate.HasValue`; a CA whose certificate cannot be found is excluded. No web changes in this item. The UI work items consume these methods.
