---
run: run-specsmd-fire-planner-02371b-015
work_item: ca-detail-info-panel
intent: ca-detail-improvements
mode: confirm
checkpoint: plan
---

# Implementation Plan: CA info panel

## Approach

On CA detail pages only, rename the "Status" panel to "Info", split the combined "Valid" row into "Not Before" and "Not After", and add a "CA kind" row. Leaf certificate pages keep today's layout.

The detail page works from the certificate slug, but purpose and parent live on the CA record. `CertificateAuthority.CertificateSlug` links them, and the service layer has no lookup by certificate slug. Rather than add a service method and a new authorization operation, the page reads `CaService.GetAllAsync()` (same roles as `CertificateDetail`: Admin, Writer, User) and picks the CA whose `CertificateSlug` matches. The CA list is small on a Pi farm and the CA list page already does the same call.

A new static helper `CaDetailInfo` holds the testable logic:
- `KindName(CertificateAuthority)`: purpose display name via `CaProfileCatalog.Get`, or "Legacy" when the CA has no purpose (same rule as `CertificateAuthorities.PurposeName`).
- `IsRoot(CertificateAuthority)`: true when `Purpose == Root`, or the CA has no usable parent (empty `ParentCaSlug` or parent equals itself), matching how `CaHierarchy` treats roots. This covers legacy CAs without a purpose.

`CertificateViewModel` gains `CaKind` (string) and `IsRootCa` (bool), set in `OnInitializedAsync` when `IsCaCertificate`. `IsRootCa` is consumed by the next work item (hide issuer tree).

## Files to Create

| File | Purpose |
|------|---------|
| `src/web/ViewModels/CaDetailInfo.cs` | Static helper: CA kind display name and root detection |
| `src/test/CaDetailInfoTest.cs` | Unit tests for the helper |

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/ViewModels/CertificateViewModel.cs` | Add `CaKind` and `IsRootCa` |
| `src/web/Pages/CertificateDetail.razor.cs` | Look up the CA by certificate slug; set `CaKind` and `IsRootCa` |
| `src/web/Pages/CertificateDetail.razor` | CA: panel title "Info", Not Before / Not After rows, CA kind row. Leaf: unchanged |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/CaDetailInfoTest.cs` | Kind name for Root, TlsSsl, Smime, Custom and no purpose ("Legacy"); root detection for Root purpose, empty parent, self parent, root-purpose with a parent, and a non-root with a parent |

## Technical Details

- If no CA record matches the certificate slug (data inconsistency), fall back to `CaKind = "Unknown"` and `IsRootCa = false` rather than failing the page.
- The test project references the web project? To be confirmed while building; if not, the helper moves next to `CaHierarchy` in `src/library/core` (namespace `Notary`) so it stays testable.
- Allman braces, NUnit 4. Verification: `dotnet build`, `dotnet test ./src/test/`, then UAT in Visual Studio.
