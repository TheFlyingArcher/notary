---
run: run-specsmd-fire-planner-01019e-005
work_item: certificates-page-create-buttons
intent: organize-certificates-by-ca
---

# Code Review: Create buttons on the Certificates page

Manual review. Auto-fixed: none. Suggestions: none.

Create Certificate is disabled while loading, when Self-signed is selected, or when there is no leaf-issuing CA, so it never links to a page that would redirect.
