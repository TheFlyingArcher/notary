---
run: run-specsmd-fire-planner-02371b-016
work_item: ca-detail-hide-root-tree
intent: ca-detail-improvements
mode: autopilot
---

# Implementation Plan: Hide the issuer tree on root CAs

## Approach

Wrap the Issuer Tree panel in `CertificateDetail.razor` in `@if (!Model.IsRootCa)` and skip building the tree for roots in `OnInitializedAsync`. `IsRootCa` is only set for CAs, so leaf certificates keep the tree.

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/Pages/CertificateDetail.razor` | Render the Issuer Tree only when not a root CA |
| `src/web/Pages/CertificateDetail.razor.cs` | Skip `PopulateIssuerTree` for root CAs |

## Tests

No new unit tests: the root decision is `CaDetailInfo.IsRoot`, already covered. The rendering is checked in Visual Studio UAT.
