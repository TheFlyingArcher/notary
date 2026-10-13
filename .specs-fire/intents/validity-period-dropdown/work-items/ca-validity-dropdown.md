---
id: ca-validity-dropdown
title: CA validity dropdown
intent: validity-period-dropdown
complexity: medium
mode: confirm
status: completed
depends_on:
  - validity-period-policy
created: 2026-10-12T00:00:00Z
run_id: run-specsmd-fire-528fef-003
completed_at: 2026-10-13T01:17:56.095Z
---

# Work Item: CA validity dropdown

## Description

Replace the "CA Validity" text field on the Create CA page with a dropdown driven by the shared validity policy.

## Acceptance Criteria

- [ ] "No Parent" shows the root list; selecting a parent shows the intermediate list
- [ ] Options display as "48 months (4 years)" with the recommended one marked and preselected
- [ ] Options exceeding the parent's expiry are disabled with a reason
- [ ] Changing the parent re-evaluates options and clears a selection that no longer fits
- [ ] Old free-text validation removed; submission computes NotAfter from the selected period
- [ ] Verified in the running UI for root and intermediate creation

## Technical Notes

src/web/Pages/CreateCertificateAuthority.razor (~line 38), its code-behind, and CreateCertificateAuthorityViewModel.LengthInMonths.

## Dependencies

- validity-period-policy
