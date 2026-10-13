---
id: leaf-validity-dropdown
title: Leaf certificate validity dropdown
intent: validity-period-dropdown
complexity: medium
mode: confirm
status: completed
depends_on:
  - validity-period-policy
created: 2026-10-12T00:00:00Z
run_id: run-specsmd-fire-528fef-004
completed_at: 2026-10-13T01:21:48.591Z
---

# Work Item: Leaf certificate validity dropdown

## Description

Replace the "Valid For" text field on the Create Certificate page with a dropdown driven by the shared validity policy, including day-based options.

## Acceptance Criteria

- [ ] Dropdown shows the leaf list with the recommended option preselected, formatted per the issue
- [ ] Options exceeding the selected issuing CA's expiry are disabled with a reason
- [ ] Changing the issuing CA re-evaluates options and clears a selection that no longer fits
- [ ] The 12-month minimum and old parent-cap validation are removed
- [ ] NotAfter is computed with AddMonths or AddDays by unit
- [ ] Verified in the running UI, including a 90-day certificate

## Technical Notes

src/web/Pages/CreateCertificate.razor(.cs) (OnSubmitAsync, ValidateExpiration) and CreateCertificateViewModel.ExpiryLength.

## Dependencies

- validity-period-policy
