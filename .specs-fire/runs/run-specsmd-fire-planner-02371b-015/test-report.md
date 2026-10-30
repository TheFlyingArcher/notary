# Test Report: CA info panel

Run: run-specsmd-fire-planner-02371b-015

## Test Results

- Full suite (`dotnet test ./src/test/`): 666 passed, 1 failed, 0 skipped (667 total)
- New `CaDetailInfoTest`: 21 cases, all passed (kind name for every purpose and for no purpose; root detection for Root purpose, empty parent, self parent, legacy CA, and child CAs)
- The one failure is pre-existing and unrelated: `ChangelogParserTest.Parse_PublishedChangelog_HasEveryReleaseNewestFirst` hardcodes the expected release list, but `CHANGELOG.md` now also contains `v1.2.20` (added by release automation in commit `cead4ca`). It fails on a clean checkout of `main`.

## Acceptance Criteria Validation

- [x] CA pages: panel titled "Info"; leaf certificates still show "Status" (razor conditional on `IsCaCertificate`)
- [x] CA pages: separate "Not Before" and "Not After" rows; combined "Valid" row only on leaf pages
- [x] CA pages: "CA kind" row from `CaProfileCatalog` display name, "Legacy" with no purpose ("Unknown" if no CA record matches)
- [x] Status, reason and key rows remain
- [x] `CertificateViewModel` has `CaKind` and `IsRootCa`, populated from the CA record
- [x] Helper `CaDetailInfo` (in `src/library/core`) is unit tested
- [x] Solution builds with 0 errors; existing tests pass except the pre-existing failure above
- [ ] Page check in Visual Studio (UAT) — pending the user

## Notes

The helper lives in `src/library/core` rather than the web project because the test project does not reference the web project.
