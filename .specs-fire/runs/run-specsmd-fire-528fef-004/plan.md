---
run: run-specsmd-fire-528fef-004
work_item: leaf-validity-dropdown
intent: validity-period-dropdown
mode: confirm
checkpoint: plan
approved_at: pending
---

# Implementation Plan: Leaf certificate validity dropdown

## Approach

Mirror the Create CA page change on the Create Certificate page.

- **Dropdown**: the "Expiration" `MudTextField` becomes a `MudSelect<ValidityPeriod>` using the leaf list (30 days, 90 days, 6, 12, 13, 24 months) from `ValidityPolicy.Evaluate(CertificateRole.Leaf, now, SelectedCa?.NotAfter)`. "Self Signed Certificate" has no issuer, so nothing is capped.
- **Display**: labels like "12 months (1 year)" and "90 days", the recommended option (12 months) tagged and preselected, options past the issuing CA's expiry disabled with the reason.
- **State**: `CreateCertificateViewModel.ExpiryLength` (int) becomes `ValidityPeriod? SelectedPeriod`. The same `RefreshValidity()` rule as the CA page (keep if still allowed, else recommended, else longest allowed, else none) runs when the issuing CA changes (end of the existing handler, which also covers the "self" path) and once on init. If no option fits, the select is disabled with an explanatory helper text and Required blocks submit.
- **Validation removed**: `ValidateExpiration` is deleted, including its 12-month minimum, which would otherwise block the 30 and 90 day options, and its old parent-cap check, which the dropdown and the server-side check now cover.
- **Submit**: `notAfter = SelectedPeriod.AddTo(notBefore)` replaces `AddMonths(ExpiryLength)`. Day periods use `AddDays`. I will also delete the unused `TimeSpan ts` local next to it.
- A comment in the model that says "expiration length in hours" is wrong and goes away with the property.

No new unit tests: the list, labels, day/month math, and capping are covered by `ValidityPolicyTest`, and the server-side rejection by the previous work item's tests. Page behavior cannot be run here (no MongoDB, NotaryAdmin login required), so the browser check is for your UAT, as with the CA page.

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/ViewModels/CreateCertificateViewModel.cs` | `ExpiryLength` replaced by `SelectedPeriod` |
| `src/web/Pages/CreateCertificate.razor` | `MudTextField` replaced by `MudSelect` |
| `src/web/Pages/CreateCertificate.razor.cs` | Options, `RefreshValidity()`, submit uses `AddTo`, `ValidateExpiration` removed |

## Tests

| Test File | Coverage |
|-----------|----------|
| (none new) | Existing `ValidityPolicyTest` (leaf list, day periods, capping) and issuance tests; full suite and solution build re-run |

## Technical Details

Same MudBlazor 9.5.0 pattern as `CreateCertificateAuthority.razor`: `ToStringFunc` for the closed label, `MudSelectItem Disabled` for capped options.

---
*Plan approved at checkpoint. Execution follows.*
