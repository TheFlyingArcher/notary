---
id: create-intermediate-ca-under-parent
title: Create intermediate CA under a parent
intent: hierarchical-ca-ordering
complexity: medium
mode: confirm
status: completed
depends_on:
  - create-root-ca-form
created: 2026-10-14T00:00:00Z
run_id: run-specs-md-fire-planner-fad6e4-001
completed_at: 2026-10-14T23:31:09.297Z
---

# Work Item: Create intermediate CA under a parent

## Description

/ca/create?parent={slug} locks the parent CA, pre-fills subject, key and validity cap from it, and removes Root from the purpose dropdown.

## Acceptance Criteria

- [ ] Parent selector is replaced by the locked parent
- [ ] Purpose dropdown has no Root option and respects the parent's profile
- [ ] Unknown slug or a parent that cannot issue CAs shows an error
- [ ] No parameter and no root flag falls back to the existing form
- [ ] Server-side enforcement unchanged; tests cover locked parent and purpose options

## Technical Notes

Same page as create-root-ca-form, hence the dependency. Use CaProfileCatalog.CheckCanIssueCas and EvaluateForParent.

## Dependencies

- create-root-ca-form
