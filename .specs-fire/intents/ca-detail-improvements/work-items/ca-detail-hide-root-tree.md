---
id: ca-detail-hide-root-tree
title: Hide the issuer tree on root CAs
intent: ca-detail-improvements
complexity: low
mode: autopilot
status: completed
depends_on:
  - ca-detail-info-panel
created: 2026-10-29T00:00:00Z
run_id: run-specsmd-fire-planner-02371b-016
completed_at: 2026-10-30T00:24:53.873Z
---

# Work Item: Hide the issuer tree on root CAs

## Description

The Issuer Tree is meaningless on a root CA, so hide it there.

## Acceptance Criteria

- [ ] The Issuer Tree panel does not render when `Model.IsRootCa` is true
- [ ] Non-root CAs and leaf certificates still show the Issuer Tree
- [ ] Solution builds cleanly and existing tests pass; checked in Visual Studio for UAT

## Technical Notes

Wrap the "Issuer Tree" `MudPaper` in `CertificateDetail.razor` with an `IsRootCa` check; optionally skip `PopulateIssuerTree` for roots. Uses the flag added by `ca-detail-info-panel`.

## Dependencies

- ca-detail-info-panel
