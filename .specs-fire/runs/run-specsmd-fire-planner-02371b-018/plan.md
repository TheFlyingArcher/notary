---
run: run-specsmd-fire-planner-02371b-018
work_item: ca-info-crl-side-by-side
intent: ca-detail-layout
mode: autopilot
---

# Implementation Plan: Info and CRL side by side

## Approach

In `CertificateDetail.razor`, move the CRL panel's `MudItem` out of its own `MudGrid` and into the Info panel's grid (CAs only), so the two share one row. On CAs both items use `xs="12" md="6"` so they stack below the tablet breakpoint; on leaf certificates the Info item keeps its fixed half width. No behavior or content changes.

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/Pages/CertificateDetail.razor` | CRL panel joins the Info grid; Info item width depends on `IsCaCertificate` |

## Tests

None: layout only. Verified by build, the existing suite and Visual Studio UAT.
