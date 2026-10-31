---
id: codeql-advanced-workflow
title: Add a path-gated CodeQL workflow
intent: selective-ci-runs
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-31T00:00:00Z
run_id: run-notary-001
completed_at: 2026-10-31T21:06:50.947Z
---

# Work Item: Add a path-gated CodeQL workflow

## Description

Add `.github/workflows/codeql.yml` for C# using the same change gate as CI. Runs on PRs only when .NET inputs change, and always on pushes to `main` and on a weekly schedule.

## Acceptance Criteria

- [ ] `codeql.yml` analyzes `csharp` with a manual or autobuild of `src/Notary.sln`, with only the permissions CodeQL needs (`security-events: write`)
- [ ] PR runs are skipped by a job-level `if` on a change-detection step when no .NET input changed, so any CodeQL check name stays passing
- [ ] Pushes to `main` and a weekly `schedule` always run the scan
- [ ] The path list matches the one in `ci-change-detection`
- [ ] Action versions are pinned to maintained major versions (`github/codeql-action@v3`)

## Technical Notes

Default setup must be switched off by the maintainer or results double up; document that as a UAT step. Reuse the detection approach from ci-change-detection (same logic, kept in sync) rather than a reusable workflow unless it stays small.

## Dependencies

(none)
