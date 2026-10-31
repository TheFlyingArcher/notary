---
run: run-notary-002
work_item: selective-ci-docs-uat
intent: selective-ci-runs
mode: autopilot
checkpoint: none
approved_at: n/a
---

# Implementation Plan: Document selective CI and the UAT checks

## Approach

Update the CI section of `docs/ci-cd.md`: the trigger table, what is and is not skipped (with the path list), the CodeQL workflow, the required-check and Default-setup settings, how to change the filters, and a UAT checklist. Docs only.

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `docs/ci-cd.md` | Selective CI, CodeQL, settings and UAT sections |

## Tests

None for docs. The path list in the docs is checked against `.github/scripts/detect-dotnet-changes.sh`.
