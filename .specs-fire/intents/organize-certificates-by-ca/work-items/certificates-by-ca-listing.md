---
id: certificates-by-ca-listing
title: Scoped certificate listing by CA
intent: organize-certificates-by-ca
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-13T00:00:00Z
---

# Work Item: Scoped certificate listing by CA

## Description

Add a way to list self-signed certificates and the leaf-issuing, non-revoked CAs for the dropdown, alongside the existing per-CA listing.

## Acceptance Criteria

- [ ] A query returns only the certificates issued by a given CA (existing GetCertificatesByCaAsync)
- [ ] A query returns only self-signed certificates
- [ ] A query returns non-revoked CAs whose profile issues leaf certificates, sorted by name
- [ ] Unit tests cover each query, including empty results

## Technical Notes

ICertificateService/ICertificateRepository, CertificateService, CaProfileCatalog.Resolve(ca).IssuesLeafCertificates (as used in CreateCertificate.razor.cs). Confirm how self-signed certs record their issuer (e.g. the 'self' slug) before choosing the filter.

## Dependencies

(none)
