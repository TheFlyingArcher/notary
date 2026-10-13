---
run: run-specsmd-fire-528fef-003
work_item: ca-validity-dropdown
intent: validity-period-dropdown
mode: confirm
checkpoint: plan
approved_at: pending
---

# Implementation Plan: CA validity dropdown

## Approach

Replace the "CA Validity" free-text field on the Create CA page with a `MudSelect` driven by `ValidityPolicy.Evaluate`.

- **Role**: no parent selected (`SelectedCa == null`) uses the root list; a selected parent uses the intermediate list.
- **Options**: each shows `Period.Format()` (e.g. "60 months (5 years)"), a "(recommended)" tag on the recommended one, and for disallowed options a disabled item with the reason ("Exceeds issuer expiry (2030-01-31)"). The cap is the selected parent CA's `NotAfter`.
- **Selection state**: the view model's `int LengthInMonths` becomes `ValidityPeriod SelectedPeriod`. A `RefreshValidity()` method preselects the recommended period when it is allowed (otherwise the longest allowed one, otherwise none) whenever the parent changes, and keeps the current choice only if it is still allowed under the new list. It runs at the end of both branches of the existing parent-changed handler, and once on init.
- **Validation**: the field is required ("Select a validity period"); the old free-text `ValidateExpiration` (value required, 12-month minimum) is deleted.
- **Submit**: `NotAfter = SelectedPeriod.AddTo(now)`. If the selected parent's remaining time makes no option allowed, the field shows a helper message ("No validity period fits within the parent CA's remaining lifetime") and the form cannot be submitted. The server-side check from the previous work item remains the backstop for the moment between page render and submit.

Pure logic stays in the already-tested `ValidityPolicy`, so the page code only wires it; there are no new unit tests. I will drive the page in the running app to confirm the behavior (needs MongoDB, so I will check first whether it can run here and tell you if not).

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/ViewModels/CreateCertificateAuthorityViewModel.cs` | `LengthInMonths` replaced by `ValidityPeriod SelectedPeriod` |
| `src/web/Pages/CreateCertificateAuthority.razor` | `MudTextField` replaced by `MudSelect` with option items and helper text |
| `src/web/Pages/CreateCertificateAuthority.razor.cs` | Role and options properties, `RefreshValidity()`, submit uses `AddTo`, old validator removed |

## Tests

| Test File | Coverage |
|-----------|----------|
| (none new) | Option generation and capping are covered by `ValidityPolicyTest`; full suite and solution build re-run; page behavior verified by running the app, otherwise left for your UAT in Visual Studio |

## Technical Details

MudBlazor 9.5.0. `MudSelect<ValidityPeriod>` with `ToStringFunc` so the closed select shows the formatted label; `MudSelectItem` with `Disabled` for capped options. `ValidityPeriod` is a record, so selection compares by value across re-renders.

---
*Plan approved at checkpoint. Execution follows.*
