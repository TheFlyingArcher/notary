---
id: ca-detail-info-panel
title: CA info panel
intent: ca-detail-improvements
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-29T00:00:00Z
run_id: run-specsmd-fire-planner-02371b-015
completed_at: 2026-10-30T00:17:32.645Z
---

# Work Item: CA info panel

## Description

On CA detail pages, rename the "Status" panel to "Info", replace the "Valid: from to to" row with separate "Not Before" and "Not After" rows, and add a row showing the CA kind (Root, TLS/SSL, S/MIME, ...). Leaf certificate pages are unchanged.

## Acceptance Criteria

- [ ] On a CA, the panel is titled "Info"; leaf certificates still show "Status"
- [ ] On a CA, "Not Before" and "Not After" are separate rows; the combined "Valid" row is gone
- [ ] On a CA, a "CA kind" row shows the purpose display name from `CaProfileCatalog`, or "Legacy" when the CA has no purpose
- [ ] The existing Status row (Valid/Expiring/Expired/Revoked), reason and key rows remain
- [ ] `CertificateViewModel` gains the CA kind and an `IsRootCa` flag, populated from the CA record
- [ ] Kind and root detection live in a small testable helper with unit tests (Root purpose, other purposes, Legacy/no purpose, self-issued CA)
- [ ] Solution builds cleanly and existing tests pass; page checked in Visual Studio for UAT

## Technical Notes

`src/web/Pages/CertificateDetail.razor` and `.razor.cs`, `src/web/ViewModels/CertificateViewModel.cs`. Load the CA via `CaService.GetAsync(slug)` (already injected) when `IsCaCertificate`. See `PurposeName` in `CertificateAuthorities.razor.cs` for the existing display-name mapping. Decide root detection: `Purpose == Root` versus issuer equals subject; the helper must handle legacy CAs with no purpose. Allman braces, NUnit 4 + Moq.

## Dependencies

None.
