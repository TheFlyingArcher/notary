---
id: selective-ci-docs-uat
title: Document selective CI and the UAT checks
intent: selective-ci-runs
complexity: low
mode: autopilot
status: completed
depends_on:
  - ci-change-detection
  - codeql-advanced-workflow
created: 2026-10-31T00:00:00Z
run_id: run-notary-002
completed_at: 2026-10-31T21:07:37.375Z
---

# Work Item: Document selective CI and the UAT checks

## Description

Update `docs/ci-cd.md` to describe the change gate, the path list, the CodeQL workflow and how to adjust the filters. Give the maintainer a short UAT list.

## Acceptance Criteria

- [ ] `docs/ci-cd.md` explains what triggers the .NET build, CodeQL and release, and what is skipped
- [ ] Documents turning off CodeQL Default setup and confirming the required check names
- [ ] UAT list: docs-only PR shows `Build and Quality Gates` as skipped but mergeable and no CodeQL run; a PR touching `src/` runs both; merge of a docs-only PR creates no tag or image

## Technical Notes

Docs only; no code or tests.

## Dependencies

- ci-change-detection
- codeql-advanced-workflow
