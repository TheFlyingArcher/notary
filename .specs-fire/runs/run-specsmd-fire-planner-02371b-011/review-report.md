---
run: run-specsmd-fire-planner-02371b-011
work_item: changelog-file-and-backfill
intent: homepage-changelog
generated: 2026-10-29T23:12:00Z
---

# Code Review: Change log file and backfill

Files reviewed: `src/web/CHANGELOG.md`, `src/web/Notary.Web.csproj`

## Auto-fixed

None needed.

## Checked, no change needed

- Only `## v<major>.<minor>.<patch> - <date>` headings start an entry, and every heading inside the notes was demoted to level 4, so the parser and workflow cannot mistake a notes heading for an entry.
- Version headings are normalised to a `v` prefix (three older tags had none), so they sort and compare consistently.
- No secrets, tokens or personal data in the file. The `@user` attributions and PR URLs were reduced to PR numbers.
- The csproj item is the standard `Content` form with copy-to-publish set; the Dockerfile copies `src/`, so the file reaches the image.

## Suggestions (not applied)

- The v1.0.15 release notes are one line and the v1.2.14 notes still contain a "What's Changed" PR list under the highlights. Left as written, since the notes belong to the releases.
