---
id: ca-hierarchy-ordering
title: CA hierarchy ordering
intent: hierarchical-ca-ordering
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-14T00:00:00Z
run_id: run-specs-md-fire-planner-fad6e4-001
completed_at: 2026-10-14T23:27:54.442Z
---

# Work Item: CA hierarchy ordering

## Description

Pure, unit-tested helper that turns the flat CA list into depth-first rows, each carrying its depth.

## Acceptance Criteria

- [ ] Output is depth-first: each CA is followed by its descendants
- [ ] Siblings sorted by name; roots sorted by name
- [ ] Multi-level chains give increasing depth
- [ ] A CA whose parent is missing is treated as a root
- [ ] A parent cycle cannot loop forever
- [ ] Unit tests cover all of the above

## Technical Notes

Put the helper in the library (src/library), not the page. Parent link is CertificateAuthority.ParentCaSlug.

## Dependencies

(none)
