---
run: run-specsmd-fire-planner-02371b-013
work_item: changelog-release-workflow
intent: homepage-changelog
generated: 2026-10-30T01:20:00Z
mode: confirm
---

# Implementation Walkthrough: Changelog release workflow

## Summary

Publishing a GitHub Release now adds that release to `src/web/CHANGELOG.md` automatically. The workflow commits straight to `main` with `[skip ci]`, so no tag or image is created. It needs a repository secret, `CHANGELOG_PUSH_TOKEN`, which the docs item explains how to make.

## Structure Overview

```text
Release published
  -> changelog.yml (skips prereleases; one at a time)
     -> checkout main with CHANGELOG_PUSH_TOKEN
     -> update-changelog.py (tag, date, notes from env; cleans and inserts; prints "added" or "unchanged")
     -> if added: commit "Add vX.Y.Z to the change log [skip ci]", push main (rebase and retry up to 3 times)
  ci.yml release job: also refuses a [skip ci] head commit
```

## Files Changed

### Created

| File | Purpose |
|------|---------|
| `.github/workflows/changelog.yml` | The workflow |
| `.github/scripts/update-changelog.py` | Notes cleanup and insertion |

### Modified

| File | Changes |
|------|---------|
| `.github/workflows/ci.yml` | `release` job `if` also requires the head commit not to contain `[skip ci]` |

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Where the edit logic lives | A Python script | Can be run and checked locally; keeps the workflow short |
| Untrusted notes | Environment variables only, headings forced to level 4+ | No shell injection, and a note cannot forge an entry |
| Stopping a tag | `[skip ci]` plus a guard in the release job | One layer is enough today; the second protects against GitHub changes |
| Duplicate release | Script reports `unchanged`, no commit | Safe to re-run |
| Prereleases | Skipped | They are not deployed |

## How to Verify

1. Create the secret `CHANGELOG_PUSH_TOKEN` (instructions arrive with `changelog-ci-docs`).
2. Publish a release and watch **Actions > Changelog**. `main` should gain a commit `Add vX.Y.Z to the change log [skip ci]`.
3. Confirm no new `v1.2.N` tag appears and **CI** does not run for that commit.

## Developer Notes

- The workflow could not be run here. Locally the script was run against a copy of the real file; the result parses with `ChangelogParser`, and the forged-heading, duplicate, empty-notes, bad-tag and bad-date cases behaved as planned.
- If the push is rejected by branch protection, the token's owner is not on the bypass list.
