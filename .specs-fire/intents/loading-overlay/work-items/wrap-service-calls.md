---
id: wrap-service-calls
title: Wrap page and dialog service calls
intent: loading-overlay
complexity: medium
mode: confirm
status: completed
depends_on:
  - loading-tracker-service
  - loading-overlay-component
created: 2026-11-07T00:00:00Z
run_id: run-specsmd-fire-planner-6274df-003
completed_at: 2026-11-08T17:59:26.112Z
---

# Work Item: Wrap page and dialog service calls

## Description

Route every awaited service call in the web pages and dialogs through `ILoadingTracker` so the overlay appears for page loads, creates, revokes and downloads.

## Acceptance Criteria

- [ ] Every awaited service call in `src/web/Pages` and `src/web/Shared` goes through the tracker (about 26 call sites: Certificates, Certificate Authorities, Certificate Detail, Create Certificate, Create Self-Signed, Create CA, Revoke and Download dialogs, and any others found)
- [ ] Reads, writes and downloads are all covered
- [ ] Wrapping does not change behavior: existing results, error handling and snackbar messages are unchanged
- [ ] Trackers are injected through constructors, not resolved ad hoc
- [ ] A grep of the pages and dialogs finds no unwrapped service call
- [ ] Build and all existing tests pass

## Technical Notes

Prefer wrapping at the call site (`await Loading.RunAsync(() => CaService.GetAllAsync())`) rather than around whole methods, so user dialogs that wait on input do not hold the overlay up. Dialogs that await user input (`dialog.Result`) must not be wrapped.

## Dependencies

- loading-tracker-service
- loading-overlay-component
