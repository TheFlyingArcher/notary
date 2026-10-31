---
id: changelog-release-workflow
title: Changelog release workflow
intent: homepage-changelog
complexity: medium
mode: confirm
status: completed
depends_on: [changelog-file-and-backfill]
created: 2026-10-29T00:00:00Z
---

# Work Item: Changelog release workflow

## Description

Add a workflow that runs when a GitHub Release is published, prepends that release's entry to `src/web/CHANGELOG.md` in the agreed format, and pushes the commit straight to `main` without creating a tag.

## Acceptance Criteria

- [ ] `.github/workflows/changelog.yml` triggers on `release: published`
- [ ] It prepends version, date and notes in the format from `changelog-file-and-backfill`
- [ ] The commit message contains `[skip ci]` and no tag is created
- [ ] `ci.yml` is checked so the push does not start the release job
- [ ] Re-running for a version already in the file does not add a duplicate
- [ ] The release notes are passed through environment variables, not interpolated into shell
- [ ] Uses a repository secret for the bypass token, not `GITHUB_TOKEN`

## Technical Notes

Existing workflows are `ci.yml` (merge to main tags and builds) and `deploy.yml`. Release notes are untrusted text, so pass them via env and write them with a script, not inline shell. Concurrency group to serialize two quick releases. Could be raised to high with a design review since it pushes to a protected branch.

## Dependencies

- changelog-file-and-backfill
