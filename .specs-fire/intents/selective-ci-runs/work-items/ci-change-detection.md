---
id: ci-change-detection
title: Skip the .NET build and release for non-code changes
intent: selective-ci-runs
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-31T00:00:00Z
run_id: run-notary-001
completed_at: 2026-10-31T21:06:31.193Z
---

# Work Item: Skip the .NET build and release for non-code changes

## Description

Add a change-detection job to `ci.yml` that decides whether the diff touches .NET inputs. Gate `quality` on it so docs-only runs skip the build, test and coverage steps, and the release job is skipped along with it.

## Acceptance Criteria

- [ ] A `changes` job outputs `dotnet` = true/false from the PR diff or push range, using the path list in the brief
- [ ] `quality` keeps its name and has a job-level `if` on that output, so a docs-only PR shows it as skipped (passing for the required check)
- [ ] Pushes to `main` still evaluate the diff of that push; an unreadable diff or first push falls back to true (run everything)
- [ ] `release` is skipped when `quality` is skipped, and the existing `[skip ci]` condition is kept
- [ ] Changing `ci.yml` itself counts as a code change
- [ ] Workflow lints clean (actionlint) and the .NET steps are unchanged

## Technical Notes

Prefer plain `git diff` over a third-party action to avoid a new dependency; `actions/checkout` needs enough history (fetch-depth) for the base. Do not use workflow-level `paths:`; it leaves required checks pending. No unit tests apply to YAML; verify with actionlint plus a docs-only and a code PR.

## Dependencies

(none)
