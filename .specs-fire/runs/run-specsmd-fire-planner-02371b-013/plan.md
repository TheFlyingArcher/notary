---
run: run-specsmd-fire-planner-02371b-013
work_item: changelog-release-workflow
intent: homepage-changelog
mode: confirm
checkpoint: plan
approved_at: pending
---

# Implementation Plan: Changelog release workflow

## Approach

A new workflow, `changelog.yml`, runs when a GitHub Release is published. A small Python script does the file edit, so it can be run and checked locally and the workflow stays short.

**Workflow** (`.github/workflows/changelog.yml`)
- Trigger: `release: types: [published]`. Prereleases are skipped.
- `permissions: contents: read` for the default token. The push uses a repository secret, `CHANGELOG_PUSH_TOKEN`, which can bypass branch protection (setup steps come in `changelog-ci-docs`).
- `concurrency: group: changelog`, not cancelling, so two quick releases are written one after the other.
- Checks out the default branch (not the tag) with the secret token, runs the script, and if the file changed commits as `github-actions[bot]` with the message `Add vX.Y.Z to the change log [skip ci]` and pushes to `main`. If the push is rejected because `main` moved, it rebases and retries up to three times.
- No tag is created. Nothing in the workflow runs `git tag`.

**Script** (`.github/scripts/update-changelog.py`)
- Reads the tag, publish date and notes from environment variables, never from the command line or shell interpolation, because release notes are untrusted text.
- The version must match `v<n>.<n>.<n>` after adding a missing `v`; anything else fails the job.
- Same cleanup as the backfill: drop the "Full Changelog" line and a leading `# vX` title, reduce "by @user in <PR URL>" to "(#PR)", and demote every heading to level 4 or deeper, so a release note can never inject its own `## vX - date` entry.
- Empty notes become `_No release notes._`.
- Inserts `## vX.Y.Z - yyyy-MM-dd` and the notes above the first existing entry (or at the end if there is none). If that version already has an entry, it changes nothing and the job ends cleanly with no commit.

**CI guard** (`.github/workflows/ci.yml`)
- The push to `main` would normally start CI and its release job, which creates a tag. `[skip ci]` in the commit message stops the push from starting CI at all. As a second layer, the release job's `if` also requires that the head commit message does not contain `[skip ci]`, so a change to how GitHub treats the token or message cannot create a stray tag.

## Files to Create

| File | Purpose |
|------|---------|
| `.github/workflows/changelog.yml` | Runs on a published release and pushes the new entry to `main` |
| `.github/scripts/update-changelog.py` | Cleans the notes and inserts the entry |

## Files to Modify

| File | Changes |
|------|---------|
| `.github/workflows/ci.yml` | Release job also requires that the head commit is not `[skip ci]` |

## Tests

| Test File | Coverage |
|-----------|----------|
| (checked by running the script locally, not committed) | New entry goes first; a second run for the same version changes nothing; headings in notes are demoted and cannot forge an entry; empty notes; bad tag fails; the result still parses with the real `ChangelogParser` into one more entry |

## Technical Details

- A workflow cannot be run from here, so the YAML is checked for syntax only. The first real run happens when you publish a release, after you create the secret from the docs.
- Because the push uses a personal or fine-grained token, it bypasses branch protection only if that token's owner is on the branch's bypass list; this is covered in the docs item.
- Out-of-order publishing (an older release published after a newer one) is added at the top; the log is ordered by when it was published, not by version.

---
*Plan awaiting approval.*
