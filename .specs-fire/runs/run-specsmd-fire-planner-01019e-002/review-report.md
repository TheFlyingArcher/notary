---
run: run-specsmd-fire-planner-01019e-002
work_item: certificates-ca-dropdown-ui
intent: organize-certificates-by-ca
---

# Code Review: CA dropdown on the Certificates page

No linter configured; manual review.

## Auto-fixed

(none)

## Suggestions

(none requiring approval)

## Observations

- Storage reads and writes are wrapped so a failure only loses the remembered choice.
- A stored CA that was revoked or removed is ignored because it must appear in the current CA list.
- Stale-result risk: a quick second selection while a load is in flight could finish out of order. Low impact for this page's data volume.
