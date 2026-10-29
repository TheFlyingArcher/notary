---
id: changelog-file-and-backfill
title: Change log file and backfill
intent: homepage-changelog
complexity: low
mode: autopilot
status: completed
depends_on: []
created: 2026-10-29T00:00:00Z
run_id: run-specsmd-fire-planner-02371b-011
completed_at: 2026-10-29T22:59:23.142Z
---

# Work Item: Change log file and backfill

## Description

Create `src/web/CHANGELOG.md` with a fixed, parseable entry format (version, date, notes) listed newest first, backfill it from the existing GitHub Releases, and make sure it is published with the web app.

## Acceptance Criteria

- [ ] `src/web/CHANGELOG.md` exists and documents its entry format in a short header comment
- [ ] Every existing GitHub Release (v1.0.15 through v1.2.19) has an entry, newest first, with version, date and notes
- [ ] `dotnet publish` output for `Notary.Web` contains `CHANGELOG.md`
- [ ] Solution builds cleanly

## Technical Notes

Use `gh release list` and `gh release view`. The Dockerfile copies only `src/`, so the file must live under `src/web/`. Add a csproj item with `CopyToPublishDirectory`. Keep the format simple enough for both the page parser and the workflow to read and write, for example `## v1.2.19 - 2026-10-29` headings.

## Dependencies

(none)
