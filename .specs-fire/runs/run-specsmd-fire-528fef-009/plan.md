---
run: run-specsmd-fire-528fef-009
work_item: leaf-profile-enforcement
intent: ca-purpose-profiles
mode: confirm
checkpoint: plan
approved_at: pending
---

# Implementation Plan: Enforce issuing CA profile on leaf certificates

## Approach

Make the server refuse a leaf certificate whose usages fall outside the issuing CA's profile, so the rule holds for anything that calls the service, not just the page. The check runs after the issuing certificate is loaded and before any key is generated.

**Core**
- `CaProfileCatalog.CheckLeaf(CaProfile issuer, IEnumerable<CertificateKeyUsage> keyUsages, IEnumerable<string> extendedKeyUsages)` returns a `Result`:
  - A CA that does not issue leaf certificates (Root) is `Invalid`: "A Root CA never issues certificates directly. Create an intermediate CA and issue from that."
  - A profile that restricts leaf usages (every named purpose) requires at least one key usage and one EKU, and every one must be in the profile's leaf set. The error names the offending usage and the CA's purpose.
  - A Custom CA that chose EKUs (no leaf key-usage restriction) requires the leaf's EKUs to be within the CA certificate's own EKUs, because a validator will reject them otherwise. Key usages stay free.
  - A CA with no purpose, or Custom with nothing chosen, is unrestricted, exactly as before.

**Data**
- `ICertificateAuthorityRepository.GetByCertificateSlugAsync(string certificateSlug)`, implemented with the existing `cert_slug` index. The issuing certificate does not know its CA record, so this is how the service finds the CA's purpose.

**Service**
- `CertificateService` takes `ICertificateAuthorityRepository` in its constructor (Autofac resolves it). For a non-CA request with a parent, it looks up the CA record by the parent certificate's slug, resolves its profile with `CaProfileCatalog.Resolve`, and runs `CheckLeaf`.
- If no CA record owns the issuing certificate (for example a certificate that is not a CA entity), the request is treated as unrestricted, as before.
- Self-signed requests and CA-certificate requests are untouched. CA creation already has its own checks.

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/core/Profiles/CaProfileCatalog.cs` | Add `CheckLeaf` |
| `src/library/core/Interface/Repository/ICertificateAuthorityRepository.cs` | Add `GetByCertificateSlugAsync` |
| `src/library/data/Repository/CertificateAuthorityRepository.cs` | Implement it |
| `src/library/service/CertificateService.cs` | New constructor dependency; leaf check before key generation |
| `src/test/InMemoryFakes.cs` | CA repository fake implements the new method |
| `src/test/CaProfileCatalogTest.cs` | Tests for `CheckLeaf` |
| `src/test/CaPurposeIssuanceTest.cs` | Real-certificate leaf tests; pass the CA repository to `CertificateService` |
| `src/test/CertificateRevokeServiceTest.cs` | Pass the CA repository to `CertificateService` |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/CaProfileCatalogTest.cs` | `CheckLeaf` for every purpose: allowed subset, EKU outside the set, key usage outside the set, empty EKUs, Root, Custom with chosen EKUs, and unrestricted CAs |
| `src/test/CaPurposeIssuanceTest.cs` | Over real certificates and real CA records: a leaf under a TLS/SSL CA with Server Authentication succeeds; the same leaf with Server and Client Authentication is rejected and nothing is saved (this is the negative case from your UAT); an extra key usage is rejected; no EKUs is rejected; a Root issuing a leaf is rejected; a self-signed leaf with any usages is unaffected; a legacy CA with no purpose is unrestricted; a Custom CA with chosen EKUs limits the leaf's EKUs |

## Technical Details

Security-sensitive: this decides which usages end up in signed leaf certificates. The earlier runs' checks (validity, child CA) use the same pattern. The existing revoke tests issue leaves under certificates that have no CA record, so they keep passing unrestricted. Verified with `dotnet build src/Notary.sln`, `dotnet test ./src/test/` and `dotnet format --verify-no-changes`.

Next after this run: `leaf-profile-ui`, which hides the usage pickers on the Create Certificate page for purpose CAs (the part you saw in UAT).

---
*Plan approved at checkpoint. Execution follows.*
