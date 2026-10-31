# Test Report: run-notary-002

## Work Item: selective-ci-docs-uat

### Test Results
- Passed: 3 (path table checked against the script's regex; `codeql.yml` still parses with `workflow_dispatch`; doc anchors exist)
- Failed: 0
- Skipped: 0

Docs only; no .NET code changed.

### Acceptance Criteria Validation
- [x] `docs/ci-cd.md` explains what triggers the .NET build, CodeQL and release, and what is skipped
- [x] Documents turning off CodeQL Default setup and the required check names
- [x] UAT list covers a docs-only PR, a code PR, and both merges
