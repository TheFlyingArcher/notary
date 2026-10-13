---
id: custom-ca-usages-ui
title: Custom CA usage pickers
intent: ca-purpose-profiles
complexity: medium
mode: confirm
status: completed
depends_on:
  - ca-purpose-selector-ui
created: 2026-10-13T00:00:00Z
run_id: run-specsmd-fire-528fef-011
completed_at: 2026-10-13T03:28:31.320Z
---

# Work Item: Custom CA usage pickers

## Description

When Custom is selected, reveal key usage and EKU pickers with a discouraged warning. Under a purpose parent the pickers are limited to the parent's allowed set.

## Acceptance Criteria

- [ ] Pickers appear only for Custom
- [ ] Warning states the choice is discouraged
- [ ] Under a purpose parent, options outside the parent's set are disabled
- [ ] Chosen usages are saved and used for the CA certificate
- [ ] At least one key usage is required (empty KU is invalid per RFC 5280)
- [ ] Verified in the running UI

## Technical Notes

Reuse the usage lists from the Create Certificate view model.

## Dependencies

- ca-purpose-selector-ui
