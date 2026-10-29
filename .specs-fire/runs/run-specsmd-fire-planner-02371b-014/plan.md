---
run: run-specsmd-fire-planner-02371b-014
work_item: changelog-ci-docs
intent: homepage-changelog
mode: autopilot
checkpoint: none
approved_at: n/a
---

# Implementation Plan: CI/CD docs for the changelog automation

## Approach

Add a "Change log" section to `docs/ci-cd.md`, between the CI and CD sections, and list the new secret under the CI repository configuration.

The section covers how the automation behaves (published releases only, no tag or image, cleaned notes, safe to re-run, the one-release lag in the deployed app) and a numbered setup for the push token. The setup follows what the repository actually has: `gh api` shows `main` is protected by a ruleset (pull request required, no deletion, no force push, empty bypass list), not classic branch protection, so the bypass step is "add Repository admin to the ruleset's bypass list" and the token is a fine-grained PAT limited to this repository with Contents read and write.

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `docs/ci-cd.md` | New "Change log" section with token setup; `CHANGELOG_PUSH_TOKEN` added to the CI secrets list |

## Tests

| Test File | Coverage |
|-----------|----------|
| (none) | Documentation only. Checked against the workflow, the script and the live ruleset. |

---
*Autopilot: no checkpoint.*
