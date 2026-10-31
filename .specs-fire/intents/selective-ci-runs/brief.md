---
id: selective-ci-runs
title: Run CI and CodeQL only for relevant changes
status: completed
created: 2026-10-31T00:00:00Z
completed_at: 2026-10-31T21:07:37.396Z
---

# Intent: Run CI and CodeQL only for relevant changes

## Goal

Run the .NET build, test and coverage gate and the CodeQL scan only when a change can affect the .NET code. PRs that only change runbooks, docs, deploy files or planning files skip them.

## Users

The maintainer and any contributor opening PRs; it also saves GitHub Actions minutes.

## Problem

`ci.yml` has no path filters, so every PR and push to `main` restores, builds, tests and runs the coverage gate. CodeQL is not in `.github/workflows`; it is most likely GitHub's Default setup, which cannot be path-filtered, so it also scans docs-only PRs.

## Success Criteria

- A PR that touches only docs, `deploy/`, `.specs-fire/`, `.specsmd/`, issue/PR templates or `README.md` skips the build, test and CodeQL work
- A PR that touches build inputs still runs everything: `src/**`, `Notary.AppHost/**`, `Notary.ServiceDefaults/**`, `.github/workflows/ci.yml`, the CodeQL workflow, and any `Directory.*.props`, `global.json` or NuGet config
- `Build and Quality Gates` is a required check, so a docs-only PR must still show it as passing, not pending (use a job-level `if` on a change-detection job, not workflow-level `paths:`, because a skipped job counts as passing and a filtered-out workflow stays pending)
- A new `codeql.yml` is gated the same way on PRs, and always runs on pushes to `main` and on a weekly schedule so a skipped scan cannot leave coverage stale
- A docs-only merge to `main` does not tag or publish a Docker image; a code merge does, with the existing versioning
- `deploy.yml` and `changelog.yml` are unchanged

## Constraints

- CodeQL Default setup must be turned off in Settings > Code security, or both scans run; this is a manual step for UAT
- Keep the `quality` job name `Build and Quality Gates`
- `src/Dockerfile` lives under `src/`, so Dockerfile changes trigger CI; `deploy/` is not built or tested in CI and does not
- Out of scope: build caching or speed-ups, changing the coverage threshold

## Notes

The changelog workflow commits with `[skip ci]`, and the release job already honours it; keep that behaviour.
