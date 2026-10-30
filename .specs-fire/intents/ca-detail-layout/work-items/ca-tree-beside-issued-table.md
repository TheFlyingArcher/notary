---
id: ca-tree-beside-issued-table
title: Issuer Tree beside Issued Certificates
intent: ca-detail-layout
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-30T00:00:00Z
run_id: run-specsmd-fire-planner-02371b-019
completed_at: 2026-10-30T23:14:45.809Z
---

# Work Item: Issuer Tree beside Issued Certificates

## Description

On CA detail pages, put the Issuer Tree in a narrow left column (about 1/3) with Issued Certificates filling the right (about 2/3). Root CAs have no tree, so their table is full width.

## Acceptance Criteria

- [ ] Non-root CA: tree about 1/3 width, Issued Certificates about 2/3, in one row; they stack full width below the tablet breakpoint
- [ ] Root CA: Issued Certificates is full width
- [ ] Leaf certificate pages: the tree keeps its current layout and there is no table
- [ ] The table's View and Download buttons, empty message and column set are unchanged
- [ ] Spacing stays consistent with the other panels, including space below the last row
- [ ] Solution builds cleanly and existing tests pass; checked in Visual Studio for UAT

## Technical Notes

`src/web/Pages/CertificateDetail.razor`: the tree is a `MudPaper` guarded by `!Model.IsRootCa`, and the table follows in its own block guarded by `IsCaCertificate`. For CAs, wrap both in one `MudGrid` using `xs="12" md="4"` for the tree and `md="8"` for the table (`md="12"` when root). Keep the bottom spacer. Layout only, so no new unit tests; verify by build and UAT. Preserve BOM and CRLF.

## Dependencies

None.
