---
run: run-specsmd-fire-planner-01019e-001
work_item: certificates-by-ca-listing
intent: organize-certificates-by-ca
generated: 2026-10-13T23:55:00Z
status: passed
---

# Test Report: Scoped certificate listing by CA

## Summary

| Category | Passed | Failed | Skipped |
|----------|--------|--------|---------|
| Unit | 309 | 0 | 0 |

7 of the 309 are new (`CertificateListingTest`). Coverage was not collected for this run.

## Acceptance Criteria Validation

- ✅ **Per-CA query returns only that issuer's certificates** — `CertificatesByCa_ReturnsOnlyThatIssuersCertificates`
- ✅ **Self-signed query returns only self-signed certificates** — `SelfSigned_ReturnsOnlyNonCaCertificatesWithoutAnIssuer` (null and empty issuer; CA certificates excluded)
- ✅ **Leaf-issuing, non-revoked CAs sorted by name** — `LeafIssuingAuthorities_*` (roots, non-issuers, revoked and missing CA certificates excluded; ordering)
- ✅ **Tests cover empty results** — `LeafIssuingAuthorities_WithNoCas_IsEmpty`, `SelfSigned_WithNoCertificates_IsEmpty`

## Notes

- The repository tests run against `FakeCertificateRepository`. The Mongo filter in `CertificateRepository.GetSelfSignedAsync` is not exercised by tests (no database in CI) and gets verified in the UI work items.
- `Notary.Web` and `Notary.Setup` build cleanly.

## Test Commands

```bash
dotnet test ./src/test/
```
