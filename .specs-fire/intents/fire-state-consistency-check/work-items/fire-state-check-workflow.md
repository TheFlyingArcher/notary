---
id: fire-state-check-workflow
title: Workflow and docs for the FIRE state check
intent: fire-state-consistency-check
complexity: medium
mode: confirm
status: completed
depends_on:
  - fire-state-check-script
  - fire-state-check-tests
created: 2026-10-31T00:00:00Z
run_id: run-notary-003
completed_at: 2026-10-31T21:54:46.837Z
---

# Work Item: Workflow and docs for the FIRE state check

## Description

Add `.github/workflows/fire-state.yml` that installs `yaml`, runs the tests and then the check, only when FIRE state or the check changes. Document it.

## Acceptance Criteria

- [ ] Triggers on `pull_request` and `push` to `main` with `paths`: `.specs-fire/**`, `.github/scripts/check-fire-state.cjs`, the tests, and the workflow file
- [ ] Steps: checkout, setup-node, `npm install --no-save yaml` in a temp location, `node --test`, then the check; read-only `contents` permission
- [ ] A failing PR shows the mismatches and the `--fix` command in the log
- [ ] It does not run for .NET-only changes, and `ci.yml`'s path list is unchanged
- [ ] `docs/ci-cd.md` has a short section: what it checks, how to fix (`node .github/scripts/check-fire-state.cjs --fix`), and that it is not a required check
- [ ] UAT: a PR that leaves an intent in_progress with all items completed fails; after `--fix` it passes

## Technical Notes

Pin actions to the same majors used elsewhere (`actions/checkout@v4`). Do not add the workflow to the required checks: a `paths`-filtered workflow stays pending when it does not run.

## Dependencies

- fire-state-check-script
- fire-state-check-tests
