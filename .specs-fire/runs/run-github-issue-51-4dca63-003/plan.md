---
run: run-github-issue-51-4dca63-003
work_item: algorithm-aware-leaf-usages, ca-certificate-usages-ec, ec-key-usage-tests
intent: ec-key-usage-rfc5480
mode: confirm
checkpoint: plan
approved_at: 2026-10-24T23:59:00Z
---

# Implementation Plan: EC key usages (RFC 5480)

## Approach

1. `CaProfile.GetLeafKeyUsages(Algorithm)`: RSA returns today's `LeafKeyUsages`; EC maps KeyEncipherment to nothing and, for S/MIME, to KeyAgreement (so EC TLS/User login/IoT/802.1x = DigitalSignature; EC S/MIME = DigitalSignature, NonRepudiation, KeyAgreement). The catalog keeps one table; the EC mapping is derived, not duplicated per profile.
2. A shared rule `CaProfileCatalog.CheckKeyUsagesForAlgorithm(Algorithm, usages)` returns an Invalid `Result` naming Key Encipherment / Data Encipherment for EC. `CheckLeaf` gains a subject-algorithm parameter, runs this rule first (so Custom and unrestricted issuers are covered), then uses the algorithm-specific permitted set for restricted issuers.
3. `CertificateService.IssueCertificateAsync` passes `request.KeyAlgorithm`. The EC rule also runs for a self-signed/no-issuer request so it cannot be bypassed.
4. `CertificateAuthorityService`: legacy path drops KeyEncipherment/DataEncipherment (keeps CrlSign, KeyCertSign, DigitalSignature) for all keys; a Custom CA with an EC key and the two bits fails with the shared rule before saving.
5. UI: `CreateCertificate` uses `GetLeafKeyUsages(ViewModel.KeyAlgorithm)` for locked usages and the note, and hides the two bits (dropping chosen ones) when EC is selected for unlocked CAs; `CreateCertificateAuthority` hides them when the effective key algorithm (own or parent's) is EC.
6. Tests for each profile with RSA and EC, issuance rejection, issued-certificate keyUsage parsing, legacy CA certificate, Custom EC CA rejection.

## Files to Create

(none)

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/core/Profiles/CaProfile.cs` | `GetLeafKeyUsages(Algorithm)` |
| `src/library/core/Profiles/CaProfileCatalog.cs` | EC rule, `CheckLeaf` algorithm parameter |
| `src/library/service/CertificateService.cs` | Pass key algorithm; enforce rule |
| `src/library/service/CertificateAuthorityService.cs` | Legacy usages; Custom EC CA check |
| `src/web/Pages/CreateCertificate.razor(.cs)` | Algorithm-aware locked usages, hide bits for EC |
| `src/web/Pages/CreateCertificateAuthority.razor(.cs)` | Hide bits for EC |
| `src/test/CaProfileCatalogTest.cs`, `CaPurposeIssuanceTest.cs`, CA service tests | Updated callers, new EC tests |

## Tests

| Test File | Coverage |
|-----------|----------|
| `CaProfileCatalogTest.cs` | EC and RSA leaf sets per profile; `CheckLeaf` EC rejection for each profile, Custom, unrestricted |
| `CaPurposeIssuanceTest.cs` | EC issuance rejection saves nothing; issued EC certificate has no encipherment bits |
| CA service tests | Legacy CA certificate has no encipherment bits (RSA, EC); Custom EC CA with the bits rejected |

## Technical Details

Existing tests that assert `LeafKeyUsages` equals DigitalSignature+KeyEncipherment stay valid for RSA (the property is unchanged). Existing certificates and CAs are not modified; they must be re-issued. UAT: create a P-384 TLS CA, issue a leaf, and check `openssl x509 -text` shows only Digital Signature.

---
*Plan approved at checkpoint. Execution follows.*
