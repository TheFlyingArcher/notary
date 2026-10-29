---
run: run-specsmd-fire-planner-02371b-014
work_item: changelog-ci-docs
intent: homepage-changelog
generated: 2026-10-30T02:00:00Z
status: passed
---

# Test Report: CI/CD docs for the changelog automation

## Summary

Documentation only; no code changed, so no tests were run or added.

## Acceptance Criteria Validation

- ✅ **Step-by-step instructions to create the token (type, scopes, expiry) and add it as a repository secret** — fine-grained PAT, one repository, Contents read and write, expiry guidance; secret `CHANGELOG_PUSH_TOKEN` matches the name used in `changelog.yml`
- ✅ **Instructions to let that token bypass branch protection on `main`** — written for the repository's actual ruleset (Settings > Rules > Rulesets > `main` > Bypass list > Repository admin), checked with `gh api`
- ✅ **Explains the trigger, the `[skip ci]` push with no tag, and the one-release lag** — all three are in the "How it behaves" list
- ✅ **Notes that unreleased merges do not appear** — first bullet

## Checks Run

- The secret name, the commit message and the `[skip ci]` behaviour in the doc match `changelog.yml` and `ci.yml`.
- `gh api repos/TheFlyingArcher/notary/rulesets` shows a ruleset named `main` on the default branch (pull request required, deletion and force push blocked, no bypass actors), which is what the doc describes.
- The link from the secrets list to the new section uses the anchor `#change-log-githubworkflowschangelogyml`.

## Not Verified

- That a fine-grained token owned by a repository admin is let through by the "Repository admin" bypass entry has not been tried. Step 5 of the setup is the check.
