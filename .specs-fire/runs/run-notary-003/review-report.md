# Code Review: run-notary-003

Files reviewed: `.github/scripts/check-fire-state.cjs`, `.github/scripts/tests/check-fire-state.test.cjs`, `.github/workflows/fire-state.yml`, `docs/ci-cd.md`.

### Auto-fixed
- Removed a test that ran the check on the real repository: it would fail the unit tests when the planning files drift, instead of the check step reporting it with a clearer message.
- Changed the test command from a directory to a glob, because `node --test <dir>` treats a directory as a module.

### Findings
- Info: `writeField` only replaces a single-line `status:` or `completed_at:` value; multi-line values are not used by FIRE.
- Info: A missing brief or work item file is skipped, by design (disk-versus-state checks are out of scope).
- Info: `pending` is accepted for an intent only when it is already `pending` and every item is `pending`, so a planned intent is not forced to `in_progress`.
- Info: Event data never reaches a shell; the workflow takes no user input.