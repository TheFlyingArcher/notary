---
id: ca-info-crl-side-by-side
title: Info and CRL side by side
intent: ca-detail-layout
complexity: low
mode: autopilot
status: completed
depends_on: []
created: 2026-10-30T00:00:00Z
run_id: run-specsmd-fire-planner-02371b-018
completed_at: 2026-10-30T23:11:48.578Z
---

# Work Item: Info and CRL side by side

## Description

On CA detail pages, put the Info panel and the Certificate Revocation List panel in one row, half width each, instead of two stacked rows with empty space beside each.

## Acceptance Criteria

- [ ] On a CA, Info and CRL share one row (each half width) and stack full width below the tablet breakpoint
- [ ] Leaf certificate pages keep today's layout (Info/Status half width, no CRL)
- [ ] Panel contents are unchanged
- [ ] Solution builds cleanly and existing tests pass; checked in Visual Studio for UAT

## Technical Notes

`src/web/Pages/CertificateDetail.razor`: the Info panel is a `MudItem xs="6"` in its own `MudGrid`, and the CRL panel is in a second `MudGrid` guarded by `IsCaCertificate`. Merge them into one grid for CAs, using `xs="12" md="6"` so they stack on small screens. The file is UTF-8 with BOM and CRLF; preserve both.

## Dependencies

None.
