# Test Report: run-notary-003

## Work Item: fire-state-check-script

### Test Results
- Passed: 2 (script exits 0 on the current repository; unknown arguments exit 2 by construction)
- Failed: 0
- Skipped: 0

### Acceptance Criteria Validation
- [x] Rule 1 and Rule 2 implemented, including `completed_at` on completed intents and briefs
- [x] Output lists file, field, expected and found; exit 1 on a mismatch, 0 otherwise; the text names the `--fix` command
- [x] `--fix` edits only `status` and `completed_at` values, keeps `state.yaml` formatting and line endings
- [x] `--root <dir>` selects the repository root
- [x] Runs clean on the current `main`

---

## Work Item: fire-state-check-tests

### Test Results
- Passed: 17 (`node --test ".github/scripts/tests/*.test.cjs"`)
- Failed: 0
- Skipped: 0

Cases: a consistent repository; an all-pending intent may stay pending; six failure cases (intent should be completed, intent completed while an item is not, brief out of date, work item file out of date, completed brief without `completed_at`, completed intent without `completed_at` in state), each checked for the message, the `--fix` hint, a passing second run after `--fix`, and that only `status` and `completed_at` lines changed; CRLF preserved; `completed_at` inserted after `created`; the rest of `state.yaml` untouched.

### Acceptance Criteria Validation
- [x] Fixtures are built in temporary directories by the tests
- [x] Pass case exits 0; each fail case exits 1 and names the file and the expected status
- [x] `--fix` makes every failing fixture pass and changes nothing else
- [x] Needs no package beyond `yaml`

---

## Work Item: fire-state-check-workflow

### Test Results
- Passed: 3 (workflow parses; triggers and four path filters present; steps in order)
- Failed: 0
- Skipped: 1 (the workflow itself can only run on GitHub; actionlint not installed)

### Acceptance Criteria Validation
- [x] Triggers on `pull_request` and `push` to `main` with the four paths
- [x] Steps: checkout, setup-node 22, `npm install --no-save --prefix` for `yaml`, `node --test`, then the check; `contents: read` only
- [x] A failing run prints the mismatches and the `--fix` command
- [x] None of the new files match the .NET path list, so `Build and Quality Gates` stays skipped
- [x] `docs/ci-cd.md` has a section with the fix command and says it is not a required check
- [ ] UAT on GitHub: see the walkthrough