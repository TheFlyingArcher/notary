---
id: fire-state-check-script
title: FIRE state check script with --fix
intent: fire-state-consistency-check
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-31T00:00:00Z
run_id: run-notary-003
completed_at: 2026-10-31T21:54:38.296Z
---

# Work Item: FIRE state check script with --fix

## Description

Add `.github/scripts/check-fire-state.cjs`. It reads `.specs-fire/state.yaml`, the intent briefs and the work item files, and reports Rule 1 and Rule 2 mismatches. `--fix` corrects them.

## Acceptance Criteria

- [ ] Rule 1: intent `status` is `completed` with `completed_at` exactly when all its work items are `completed`, otherwise `in_progress`
- [ ] Rule 2: each brief and work item front-matter `status` equals the `state.yaml` status; a completed brief has `completed_at`
- [ ] Output lists file, expected and found per mismatch; exit code 1 on any, 0 otherwise; the failure text names the `--fix` command
- [ ] `--fix` rewrites only status and `completed_at` values (front matter and `state.yaml`) and keeps all other text and line endings intact
- [ ] `--root <dir>` selects the repository root (default: current directory) so tests can use fixtures
- [ ] Runs clean on the current `main`

## Technical Notes

Use the `yaml` package (Document API) to keep `state.yaml` formatting; edit front matter with a targeted line replace. A work item or brief file missing on disk is skipped, not an error (out of scope). Allman is for C#; follow the style of the existing scripts in `.github/scripts`.

## Dependencies

(none)
