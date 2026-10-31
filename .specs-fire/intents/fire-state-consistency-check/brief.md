---
id: fire-state-consistency-check
title: CI check for FIRE state consistency
status: completed
created: 2026-10-31T00:00:00Z
completed_at: 2026-10-31T21:54:46.862Z
---

# Intent: CI check for FIRE state consistency

## Goal

Fail a pull request when the FIRE bookkeeping disagrees with itself, so a finished intent cannot stay `in_progress` and work item or brief files cannot drift from `state.yaml`.

## Users

The maintainer and any contributor or agent that changes `.specs-fire/`.

## Problem

12 intents stayed `in_progress` in `state.yaml` and 12 intent or work item files said `pending` or `in_progress` for work that was done. `complete-run.cjs` never closes the intent in `state.yaml`, and work finished outside a run was never recorded. Nobody noticed until the cleanup in #92.

## Success Criteria

- Rule 1: an intent is `completed`, with `completed_at`, exactly when every one of its work items is `completed`; otherwise it is `in_progress`
- Rule 2: each `brief.md` and each `work-items/*.md` front-matter `status` equals the status in `state.yaml`; a completed brief has `completed_at`
- The check lists every mismatch with file, expected value and found value, and exits non-zero
- A `--fix` mode corrects statuses (and `completed_at`) locally; it never creates or deletes anything
- It passes on the current `main`
- A new workflow runs it in Node, only when `.specs-fire/**`, the script or the workflow changes; the .NET build and CodeQL stay skipped for those PRs

## Constraints

- Node script under `.github/scripts/`, using the `yaml` package like the FIRE scripts; installed in the job, no `package.json` added to the repo
- Not a required status check: a `paths`-filtered workflow would leave a required check pending on other PRs
- Out of scope: disk-versus-state membership checks, dependency checks, requiring a `run_id` on completed items
- Tests cover the pass, fail and `--fix` cases and run in the same workflow
- `complete-run.cjs` does not close the intent in `state.yaml`, so the last run of an intent fails Rule 1 until `--fix` is run; the failure message says so. The script gap itself is reported upstream, not patched here

## Notes

Source: the status cleanup in #92. Follows the path-gated CI work in #91.
