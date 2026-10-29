---
run: run-specsmd-fire-planner-02371b-011
work_item: changelog-file-and-backfill
intent: homepage-changelog
mode: autopilot
checkpoint: none
approved_at: n/a
---

# Implementation Plan: Change log file and backfill

## Approach

Create `src/web/CHANGELOG.md` with a fixed format that both the homepage parser and the release workflow can rely on:

- A leading HTML comment documents the format.
- One entry per release, newest first, each starting with a level-2 heading `## v<major>.<minor>.<patch> - <yyyy-MM-dd>`. That heading is the only thing that starts a new entry.
- The notes under it are Markdown. Any headings inside the notes are demoted to level 4 so they can never be mistaken for an entry heading.

Backfill from the six GitHub Releases (v1.0.15, v1.0.18, v1.1.19, v1.1.29, v1.2.14, v1.2.19). Dates come from each release's `publishedAt`. Tags are normalised to a `v` prefix (three older tags have none). Notes are kept as written, except that the redundant title line and the "Full Changelog" compare links are dropped and "by @user in <PR URL>" attributions are reduced to the PR number.

Add the file to `Notary.Web.csproj` as `Content` with `CopyToOutputDirectory` and `CopyToPublishDirectory` set to `PreserveNewest`, so it ships beside the app. It lives under `src/web/` because the Dockerfile copies only `src/`.

## Files to Create

| File | Purpose |
|------|---------|
| `src/web/CHANGELOG.md` | Change log read by the homepage and updated by the release workflow |

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/Notary.Web.csproj` | Include `CHANGELOG.md` in build output and publish output |

## Tests

| Test File | Coverage |
|-----------|----------|
| (none) | Content and packaging only. Verified by `dotnet publish` output listing and a script check of entry headings and ordering. The parser tests belong to `changelog-homepage`. |

---
*Autopilot: no checkpoint.*
