---
run: run-notary-003
work_item: fire-state-check-script
intent: fire-state-consistency-check
mode: confirm
checkpoint: plan
approved_at: pending
---

# Implementation Plan: FIRE state check (script, tests, workflow)

## Approach

A Node script reads `.specs-fire/state.yaml`, each intent's `brief.md` and each work item file, compares statuses, and reports mismatches. `--fix` corrects them. Tests use temporary fixture repositories. A path-filtered workflow runs the tests and then the check.

**Rules**
- Rule 1 (state.yaml): an intent with all work items `completed` must be `completed` and have `completed_at`. Otherwise it must not be `completed`: `in_progress` is expected, and `pending` is accepted only when every item is `pending`. An intent with no work items is skipped.
- Rule 2 (files): the `status` in each `brief.md` equals the intent status in `state.yaml`; each work item file's `status` equals that item's status in `state.yaml`; a completed brief has `completed_at`. A file missing on disk is skipped (out of scope).

**Script behaviour**
- `node .github/scripts/check-fire-state.cjs [--fix] [--root <dir>]`. Default root is the current directory.
- Output: one line per mismatch, `<file>: <field> expected '<x>' found '<y>'`; a final line `Fix with: node .github/scripts/check-fire-state.cjs --fix`; exit 1. No mismatches: prints `FIRE state is consistent.` and exits 0.
- `--fix` applies the same corrections and then prints what changed. It edits only `status` and `completed_at` values:
  - `state.yaml` via the `yaml` Document API, so formatting stays.
  - Front matter via a targeted line replace, leaving the rest of each file and its line endings untouched. `completed_at` is added after `created:` (or `status:`) if absent.
  - `completed_at` for a newly completed intent: the existing brief value, else the latest work item `completed_at`, else the current time.
- `state.yaml` is treated as the source of truth for work items; the script never changes an item's status in `state.yaml`.

**Tests** (`.github/scripts/tests/check-fire-state.test.cjs`, `node --test`): fixtures are built in `fs.mkdtempSync` directories by a small helper. Cases: consistent; intent should be completed; completed intent with a not-completed item; brief out of date; work item file out of date; completed brief missing `completed_at`; `--fix` repairs each case, a second run passes, and a before/after diff touches only status and `completed_at` lines; CRLF files keep CRLF.

**Workflow** (`.github/workflows/fire-state.yml`, name `FIRE State`): triggers on `pull_request` and `push` to `main` with `paths`: `.specs-fire/**`, `.github/scripts/check-fire-state.cjs`, `.github/scripts/tests/**`, and the workflow file. One job: checkout, `actions/setup-node@v4` (Node 22), `npm install --no-save --prefix "$RUNNER_TEMP/deps" yaml` with `NODE_PATH` set, `node --test .github/scripts/tests`, then the check. `permissions: contents: read`. Not a required check (a path-filtered workflow stays pending when it does not run). `docs/ci-cd.md` gets a short section.

## Files to Create

| File | Purpose |
|------|---------|
| `.github/scripts/check-fire-state.cjs` | The check and `--fix` |
| `.github/scripts/tests/check-fire-state.test.cjs` | Pass, fail, `--fix` and CRLF tests on temporary fixtures |
| `.github/workflows/fire-state.yml` | Path-filtered workflow that runs the tests and the check |

## Files to Modify

| File | Changes |
|------|---------|
| `docs/ci-cd.md` | Section on the FIRE state check, its `--fix` command, and that it is not a required check |
| `.specs-fire/intents/fire-state-consistency-check/*` | Statuses updated by the run scripts (and by `--fix` if needed) |

## Tests

| Test File | Coverage |
|-----------|----------|
| `.github/scripts/tests/check-fire-state.test.cjs` | Rules 1 and 2, `--fix`, `--root`, exit codes, line endings; plus the script run on this repository must pass |

## Technical Details

- No .NET changes; `dotnet test` is unaffected. The workflow only runs for `.specs-fire/**` and the check's own files, so it will not run on .NET-only PRs and `ci.yml`'s path list is unchanged.
- Since this PR touches `.specs-fire/**` and `.github/scripts/**`, the new workflow runs on it. `detect-dotnet-changes.sh` only matches that one script, so `Build and Quality Gates` stays skipped unless I touch something it lists.
- Known gap: `complete-run.cjs` does not close the intent in `state.yaml`, so the last run of any intent fails Rule 1 until `--fix` runs. I will run `--fix` before committing this intent's own completion.
- `yaml` stays out of the repository (installed in the workflow, and in the scratchpad for local runs).

---
*Plan awaiting approval.*
