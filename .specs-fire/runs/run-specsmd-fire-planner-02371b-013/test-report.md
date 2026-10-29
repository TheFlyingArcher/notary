---
run: run-specsmd-fire-planner-02371b-013
work_item: changelog-release-workflow
intent: homepage-changelog
generated: 2026-10-30T01:10:00Z
status: passed
---

# Test Report: Changelog release workflow

## Summary

No C# changed in this item, so the NUnit suite was not re-run (it was 651 of 651 passing at the end of the previous item). The workflow cannot run outside GitHub, so the script was exercised locally and the YAML was checked for syntax. Nothing here is committed as a test.

## Acceptance Criteria Validation

- ✅ **`.github/workflows/changelog.yml` triggers on `release: published`** — `on.release.types: [published]`; prereleases are skipped by the job's `if`
- ✅ **Prepends version, date and notes in the agreed format** — script run on a copy of the real file: new entry at the top, heading `## v1.2.20 - 2026-10-30`
- ✅ **Commit message has `[skip ci]` and no tag is created** — message is `Add vX.Y.Z to the change log [skip ci]`; no `git tag` anywhere in the workflow
- ✅ **`ci.yml` checked so the push does not start the release job** — the `release` job now also requires the head commit not to contain `[skip ci]`
- ✅ **Re-running for a version already in the file adds no duplicate** — second run printed `unchanged`
- ✅ **Release notes go through environment variables, not shell** — notes, tag and date reach the script only via `env`; the step line uses only quoted variables
- ✅ **Uses a repository secret, not `GITHUB_TOKEN`, for the push** — `CHANGELOG_PUSH_TOKEN` on checkout; default token is `contents: read`

## Checks Run

| Case | Result |
|------|--------|
| Notes with `##`/`###` headings, a PR attribution, CRLF line endings, a forged `## v9.9.9 - 2026-01-01` line and a "Full Changelog" line | Headings demoted to level 4, attribution became `(#90)`, forged line became a level-4 heading, "Full Changelog" dropped |
| Same version twice | `unchanged`, file not rewritten |
| Empty notes and a tag without `v` (`1.2.21`) | `v1.2.21` entry with `_No release notes._` |
| Tag `v1.2.3; rm -rf /` | Exit 1 with a message, file untouched |
| Date `nope` | Exit 1 with a message |
| Edited file read by the real `ChangelogParser` | 8 entries (2 new + 6), newest first; the forged version is not an entry |
| YAML syntax of `changelog.yml` and `ci.yml` | Parses |

## Not Verified

- The workflow itself has not run on GitHub. The first real run is when a release is published after `CHANGELOG_PUSH_TOKEN` exists.
- Whether the token bypasses branch protection depends on its owner being on the bypass list (docs item).
