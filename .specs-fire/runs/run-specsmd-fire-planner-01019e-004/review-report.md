---
run: run-specsmd-fire-planner-01019e-004
work_item: self-signed-certificate-form
intent: organize-certificates-by-ca
---

# Code Review: Self-signed certificate form

No linter configured; manual review.

## Auto-fixed

(none)

## Suggestions

(none requiring approval)

## Observations

- The new page duplicates most of the form markup from Create Certificate. Only the validators are shared. A shared form component is a reasonable follow-up once the UI can be exercised.
- The page is reachable only by URL until the buttons work item links to it.
