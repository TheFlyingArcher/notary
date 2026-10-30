---
id: ca-issued-certificates-table
title: Issued certificates table
intent: ca-detail-improvements
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-29T00:00:00Z
run_id: run-specsmd-fire-planner-02371b-017
completed_at: 2026-10-30T00:45:20.273Z
---

# Work Item: Issued certificates table

## Description

On every CA detail page (root or not), below where the Issuer Tree sits, list the certificates the CA issued directly, with view and download actions per row.

## Acceptance Criteria

- [ ] A table lists the CA's directly issued certificates (not descendants), loaded via `CertificateService.GetCertificatesByCaAsync`
- [ ] Columns: Name, Common Name, Expiry, Status (Valid/Expiring/Expired/Revoked), and an actions column
- [ ] View navigates to `/certificate?slug=...` for that certificate
- [ ] Download opens the existing `DownloadCertificateDialog` for that certificate
- [ ] A CA with no issued certificates shows an empty-state message
- [ ] The table is not shown on leaf certificate pages
- [ ] Status logic is shared with the Info panel, not duplicated, and has unit tests (valid, expiring, expired, revoked)
- [ ] Authorization follows existing role rules; no new unguarded service methods
- [ ] Solution builds cleanly and existing tests pass; page checked in Visual Studio for UAT

## Technical Notes

`src/web/Pages/CertificateDetail.razor` and `.razor.cs`; `GetCertificatesByCaAsync` exists in `CertificateService` (see `CertificateListingTest`). Reuse the download dialog parameters (`Slug`, `IsCaCertificate`). Use a MudBlazor `MudTable`/`MudDataGrid` consistent with `Certificates.razor`. Pull the status calculation into a testable helper. Allman braces, NUnit 4 + Moq.

## Dependencies

None.
