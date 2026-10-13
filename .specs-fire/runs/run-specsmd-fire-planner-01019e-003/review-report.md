---
run: run-specsmd-fire-planner-01019e-003
work_item: create-certificate-scoped-to-ca
intent: organize-certificates-by-ca
---

# Code Review: Create Certificate scoped to the selected CA

No linter configured; manual review.

## Auto-fixed

(none)

## Suggestions

(none requiring approval)

## Observations

- The redirect path leaves `_isLoading` true; harmless because the page navigates away.
- The `ca` value is a slug in the query string; the Certificates page buttons item must URL-escape it.
- Self-signed creation has no UI entry until `self-signed-certificate-form` lands (expected, noted in the plan).
