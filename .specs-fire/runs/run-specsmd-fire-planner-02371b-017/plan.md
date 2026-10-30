---
run: run-specsmd-fire-planner-02371b-017
work_item: ca-issued-certificates-table
intent: ca-detail-improvements
mode: confirm
checkpoint: plan
---

# Implementation Plan: Issued certificates table

## Approach

On every CA detail page (root or not), add an "Issued Certificates" panel below the Issuer Tree position, listing what the CA issued directly. Leaf certificate pages do not get it.

Data: `CertificateService.GetCertificatesByCaAsync(c.Slug)`. Despite the parameter name it is keyed by the CA's certificate slug (the Certificates page passes `ca.CertificateSlug`), which is the slug this page already has. It is authorized as `CertificateList`, the operation the Certificates page uses. It returns every certificate whose issuer is this CA. On a root CA the table keeps all of them, so its child CAs are listed (a root issues only CAs). On any other CA the table lists leaf certificates only: certificates with `IsCaCertificate` set are filtered out. Descendants further down are not included.

Status: a small helper `CertificateStatus.Get(notAfter, revocationDate, utcNow)` returns Valid / Expiring / Expired / Revoked with the same precedence the page uses today (Expired, then Expiring, then Revoked, then Valid; Expiring means within 30 days of expiry). The page's existing Expired/Expiring fields and the table both use it, so the rule lives in one place.

Table (MudTable, as on `Certificates.razor`): Name, Common Name (subject CN), Expiry (NotAfter), Status (coloured text), and an actions column with View (link to `certificate?slug=`) and Download (opens `DownloadCertificateDialog` with the row's slug and `IsCaCertificate`; disabled when revoked, as elsewhere). An empty CA shows "This CA has not issued any certificates." Rows are ordered by name.

## Files to Create

| File | Purpose |
|------|---------|
| `src/library/core/CertificateStatus.cs` | Status enum and calculation shared by the info panel and the table |
| `src/test/CertificateStatusTest.cs` | Unit tests: valid, expiring boundary, expired, revoked, precedence |
| `src/library/core/IssuedCertificateFilter.cs` | Keeps child CAs for root CAs, leaf certificates only otherwise |
| `src/test/IssuedCertificateFilterTest.cs` | Unit tests: root keeps CAs and leaves, non-root drops CAs |

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/ViewModels/CertificateViewModel.cs` | Add `IssuedCertificates` list (row model: slug, name, common name, expiry, status, IsCaCertificate, revoked) |
| `src/web/Pages/CertificateDetail.razor.cs` | Load issued certificates for CAs; compute Expired/Expiring via the helper; row download handler |
| `src/web/Pages/CertificateDetail.razor` | Issued Certificates panel with table, status colours, empty message |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/CertificateStatusTest.cs` | All four statuses, the 30-day boundary, expired-and-revoked precedence |

The listing query itself is already covered by `CertificateListingTest`; table rendering is checked in Visual Studio UAT.

## Technical Details

- Root CAs list their child CAs; non-root CAs list leaf certificates only (user-approved edit). The filter is a small tested helper (`IssuedCertificateFilter`) next to `CertificateStatus` in `src/library/core`.
- Authorization: reuses the existing `CertificateList` operation; no new service methods.
- Allman braces, NUnit 4. Verification: build, `dotnet test ./src/test/`, UAT.
