---
run: run-specsmd-fire-planner-02371b-011
work_item: changelog-file-and-backfill
intent: homepage-changelog
generated: 2026-10-29T23:15:00Z
mode: autopilot
---

# Implementation Walkthrough: Change log file and backfill

## Summary

Added `src/web/CHANGELOG.md`, backfilled with the six existing GitHub Releases, and made the web project publish it. It defines the entry format that the homepage parser (`changelog-homepage`) and the release workflow (`changelog-release-workflow`) both depend on.

## Structure Overview

- `src/web/CHANGELOG.md`: a header comment documenting the format, then one entry per release, newest first.
- Each entry starts with `## v<major>.<minor>.<patch> - <yyyy-MM-dd>`. That is the only line that starts an entry. Headings inside the notes are level 4 or deeper.
- The file lives under `src/web/` because `src/Dockerfile` copies only `src/` into the build context.

## Files Changed

### Created

| File | Purpose |
|------|---------|
| `src/web/CHANGELOG.md` | Change log backfilled from the six GitHub Releases |

### Modified

| File | Changes |
|------|---------|
| `src/web/Notary.Web.csproj` | `Content` item with `CopyToOutputDirectory` and `CopyToPublishDirectory` set to `PreserveNewest` |

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| File location | `src/web/CHANGELOG.md` | Reaches the Docker build, `dotnet publish` and Visual Studio runs |
| Entry boundary | Exact `## v... - date` heading | Simple to parse and to prepend to |
| Notes headings | Demoted to level 4 | Release notes use `##` and `###`, which would otherwise look like entries |
| Dates | Author's date from the release title where given, else the publish date | The UTC publish times put two releases a day late |
| Version prefix | Always `v` | Three older tags have no prefix |

## Verification

- `dotnet publish` output contains `CHANGELOG.md`, byte-identical to the source.
- Six entry headings, in descending date order.
- Existing suite: 634 passed, 0 failed.

## Developer Notes

- Release notes were lightly cleaned: the title line and "Full Changelog" compare links were dropped, and "by @user in <URL>" became "(#PR)". Wording is otherwise untouched.
- The v1.2.14 entry still has a "What's Changed" PR list under its highlights. It is left as written.
- `changelog-release-workflow` must apply the same cleaning rules (demote headings) when it prepends an entry.
