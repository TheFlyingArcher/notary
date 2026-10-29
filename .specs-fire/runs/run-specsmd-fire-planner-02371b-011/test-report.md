---
run: run-specsmd-fire-planner-02371b-011
work_item: changelog-file-and-backfill
intent: homepage-changelog
generated: 2026-10-29T23:10:00Z
status: passed
---

# Test Report: Change log file and backfill

## Summary

| Category | Passed | Failed | Skipped | Coverage |
|----------|--------|--------|---------|----------|
| Unit | 634 | 0 | 0 | n/a |
| Integration | 0 | 0 | 0 | n/a |
| **Total** | 634 | 0 | 0 | n/a |

This item adds content and a project item only, with no new code, so no new tests were written. The existing suite was run as a regression check.

## Acceptance Criteria Validation

- ✅ **`src/web/CHANGELOG.md` exists and documents its entry format in a header comment** — the comment states the heading format and the level-4 rule for headings inside notes
- ✅ **Every existing GitHub Release has an entry, newest first, with version, date and notes** — six entries: v1.2.19, v1.2.14, v1.1.29, v1.1.19, v1.0.18, v1.0.15
- ✅ **`dotnet publish` output for `Notary.Web` contains `CHANGELOG.md`** — published file is byte-identical to the source
- ✅ **Solution builds cleanly** — Release publish succeeded; the only warnings are existing CS8618 nullable warnings in unrelated files

## Notes

- Dates for v1.1.19 and v1.0.18 use the author's dates from the release titles (2024-02-06, 2024-01-30), because the UTC publish timestamps fall a day later.
- The v1.0.15 release title says 2014; the publish date shows 2024, so 2024-01-20 is used.

## Test Commands

```bash
dotnet test src/test/Notary.Test.csproj -c Release --nologo
dotnet publish src/web/Notary.Web.csproj -c Release -o <dir>
```
