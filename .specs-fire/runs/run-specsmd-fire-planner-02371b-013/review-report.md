---
run: run-specsmd-fire-planner-02371b-013
work_item: changelog-release-workflow
intent: homepage-changelog
generated: 2026-10-30T01:15:00Z
---

# Code Review: Changelog release workflow

Files reviewed: `.github/workflows/changelog.yml`, `.github/scripts/update-changelog.py`, `.github/workflows/ci.yml`

## Auto-fixed

- `changelog.yml`: `echo "result=$(python3 ...)"` would have hidden a script failure, because `echo` succeeds. The script now runs in its own assignment so a non-zero exit fails the step.
- `changelog.yml`: the commit message used the tag without its `v`; it now reads `Add vX.Y.Z to the change log`.

## Checked, no change needed

- Injection: release body, tag and date are passed only as environment variables and are quoted wherever the shell sees them. The tag and date are validated against strict patterns before use, and the notes cannot start an entry because every heading is forced to level 4 or deeper.
- The token is used only for the checkout step, and the job runs no third-party code beyond `actions/checkout`.
- The workflow checks out the default branch, not the release tag, so it edits the latest file.
- Loop and stray tag: the commit has `[skip ci]`, and the `release` job in `ci.yml` also refuses a `[skip ci]` head commit.
- Concurrency: one `changelog` group, not cancelling, so two quick releases queue.
- The script writes LF line endings, like the rest of the file, and ends it with a single newline.

## Suggestions (not applied)

- A release published out of order (older version after a newer one) goes to the top. The log is ordered by publish time, which is what the plan said.
- A fenced code block in notes that starts a line with `## ` also gets demoted to `####`. This is cosmetic and safer than the alternative.
- `actions/checkout@v4` is pinned to a major tag, like the other workflows.
