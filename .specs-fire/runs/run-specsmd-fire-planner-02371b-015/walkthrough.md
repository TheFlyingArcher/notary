# Walkthrough: CA info panel

Run: run-specsmd-fire-planner-02371b-015 · Work item: ca-detail-info-panel · Intent: ca-detail-improvements

## Summary

On CA detail pages the "Status" panel is now "Info" and shows the CA kind, Not Before and Not After as separate rows. Leaf certificate pages are unchanged. `IsRootCa` is now on the view model for the next item (hide the issuer tree on roots).

## Changes

- `src/library/core/CaDetailInfo.cs` (new): `KindName` (profile display name, "Legacy" with no purpose) and `IsRoot` (Root purpose, or no parent / self parent, like `CaHierarchy`).
- `src/web/Pages/CertificateDetail.razor.cs`: `PopulateCaInfoAsync` finds the CA record by `CertificateSlug` via `CaService.GetAllAsync()` and sets `CaKind` and `IsRootCa`; "Unknown" if no record matches.
- `src/web/Pages/CertificateDetail.razor`: panel title and rows switch on `IsCaCertificate`.
- `src/web/ViewModels/CertificateViewModel.cs`: `CaKind`, `IsRootCa`.
- `src/test/CaDetailInfoTest.cs` (new): 16 test cases.

## Decisions

- Looked the CA up through the existing list call instead of adding a service method, avoiding a new authorization operation; the roles match the page's.
- Helper placed in `src/library/core` (not web) because the test project does not reference the web project.

## Deviations

None. Coverage in the run log is an estimate: the helper's branches are all exercised.

## Verify

1. Open a root CA from the CA list: panel reads "Info", shows CA kind "Root", Not Before and Not After.
2. Open an intermediate (for example TLS/SSL): kind shows its purpose.
3. Open a leaf certificate: panel still reads "Status" with the "Valid: from to to" row.

## Developer notes

`ChangelogParserTest.Parse_PublishedChangelog_HasEveryReleaseNewestFirst` already fails on `main`: it hardcodes the release list and `CHANGELOG.md` now includes `v1.2.20`.
