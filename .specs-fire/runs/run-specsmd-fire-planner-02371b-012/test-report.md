---
run: run-specsmd-fire-planner-02371b-012
work_item: changelog-homepage
intent: homepage-changelog
generated: 2026-10-30T00:20:00Z
status: passed
---

# Test Report: Homepage shows the change log

## Summary

| Category | Passed | Failed | Skipped | Coverage |
|----------|--------|--------|---------|----------|
| Unit | 651 | 0 | 0 | n/a |
| Integration | 0 | 0 | 0 | n/a |
| **Total** | 651 | 0 | 0 | n/a |

17 tests are new (`ChangelogParserTest`). The other 634 are the existing suite, all still passing.

## Acceptance Criteria Validation

- ✅ **`/` lists releases newest first, back to v1.0.15** — the page iterates the parsed entries in file order; `Parse_PublishedChangelog_HasEveryReleaseNewestFirst` confirms the real file gives 6 entries, v1.2.19 down to v1.0.15, in descending date order
- ✅ **Release notes render as Markdown** — checked with a throwaway console run of the same Markdig pipeline: headings, lists, bold, code and auto-links render
- ✅ **Tiles, lorem ipsum, "Please wait" gate and `ICertificateService` use are gone from `Index`** — `Index.razor` and `Index.razor.cs` rewritten with none of them
- ✅ **A missing or empty `CHANGELOG.md` shows a friendly message** — `ChangelogProvider` returns a failed `Result` for a missing or unreadable file, and the page shows an alert for that and for an empty list
- ✅ **Parser tests cover multiple entries, ordering, and a malformed or empty file** — see below
- ✅ **Solution builds cleanly and existing tests pass** — Release build has no errors; 651 of 651 pass
- ⏳ **Page checked in Visual Studio for UAT** — left to you; the app needs Auth0 and MongoDB, so it was not run here

## Tests Written

### Unit Tests

- `src/test/ChangelogParserTest.cs` — null, empty and whitespace input; header-only file; version, date and notes; file order; level-4 headings stay in the notes; entry with no notes; six malformed headings are not entries; a bad date stays in the previous notes; Windows line endings; the published `CHANGELOG.md`

## Other Checks

- `dotnet publish` output contains `CHANGELOG.md` and `Markdig.dll`.
- Raw HTML in notes is escaped, not passed through: `<script>` came out as `&lt;script&gt;`.

## Test Commands

```bash
dotnet test src/test/Notary.Test.csproj -c Release --nologo
```
