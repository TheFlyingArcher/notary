---
id: fire-state-check-tests
title: Tests for the FIRE state check
intent: fire-state-consistency-check
complexity: medium
mode: confirm
status: completed
depends_on:
  - fire-state-check-script
created: 2026-10-31T00:00:00Z
run_id: run-notary-003
completed_at: 2026-10-31T21:54:46.108Z
---

# Work Item: Tests for the FIRE state check

## Description

Add tests that run the script against small fixture repositories covering the pass, fail and `--fix` cases.

## Acceptance Criteria

- [ ] Fixtures under `.github/scripts/tests/fire-state/` (or created in a temp directory by the tests): consistent, intent wrongly in_progress, brief or work item file out of date, completed brief missing `completed_at`
- [ ] Pass case exits 0 with no output about mismatches
- [ ] Each fail case exits 1 and names the file and the expected status
- [ ] `--fix` makes every failing fixture pass on a second run and changes nothing else (diff limited to status and `completed_at` lines)
- [ ] Tests run with `node --test` and need no packages beyond `yaml`

## Technical Notes

Use `node:test` and `node:assert`; build fixtures in `fs.mkdtempSync` to avoid committed junk.

## Dependencies

- fire-state-check-script
