---
run: run-specsmd-fire-528fef-001
work_item: validity-period-policy
intent: validity-period-dropdown
mode: confirm
checkpoint: plan
approved_at: pending
---

# Implementation Plan: Shared validity period policy

## Approach

Add a small, UI-agnostic validity model to `Notary.Core` so the web pages (and later the services) share one definition of what periods are offered and whether they fit under an issuer.

- `ValidityUnit` enum (Months, Days) and `CertificateRole` enum (RootCa, IntermediateCa, Leaf).
- `ValidityPeriod(int Amount, ValidityUnit Unit)` positional record: `AddTo(DateTime start)` (AddMonths/AddDays) and `Format()`.
  - Months: `"48 months (4 years)"`, `"12 months (1 year)"`, `"1 month"`; years shown only when the amount is a whole number of years, so `"6 months"` and `"13 months"` stay plain.
  - Days: `"90 days"`, `"1 day"`.
- `ValidityOption(ValidityPeriod Period, string Label, bool IsRecommended, bool IsAllowed, string DisabledReason)` positional record, the shape a dropdown binds to.
- `ValidityPolicy` static class: `GetPeriods(role)`, `GetRecommended(role)`, and `Evaluate(role, now, issuerNotAfter)` returning options with the cap applied. `issuerNotAfter == null` (self-signed root) allows everything. A period whose end date is later than the issuer's NotAfter is disallowed with reason `"Exceeds issuer expiry (<date>)"`. Equal to the issuer's NotAfter is allowed.

Draft period lists (please approve or amend):

| Role | Options | Recommended |
|------|---------|-------------|
| Root CA | 120, 180, 240 months (10, 15, 20 years) | 240 months |
| Intermediate CA | 36, 60, 120 months (3, 5, 10 years) | 60 months |
| Leaf | 30 days, 90 days, 6, 12, 13, 24 months | 12 months |

Nothing in the UI or services changes in this item; later items consume the policy.

## Files to Create

| File | Purpose |
|------|---------|
| `src/library/core/Validity/ValidityPeriod.cs` | `ValidityUnit` + `ValidityPeriod` record, date math and formatting |
| `src/library/core/Validity/CertificateRole.cs` | Role enum used to pick the option list |
| `src/library/core/Validity/ValidityOption.cs` | Option record for dropdown binding |
| `src/library/core/Validity/ValidityPolicy.cs` | Per-role lists, recommended default, cap evaluation |
| `src/test/ValidityPolicyTest.cs` | NUnit tests |

## Files to Modify

| File | Changes |
|------|---------|
| (none) | |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/ValidityPolicyTest.cs` | Each role has exactly one recommended option; lists match the approved table; formatting (whole years, singular, non-whole months, days); no issuer allows all; options beyond issuer NotAfter disallowed with reason; boundary equal to issuer NotAfter allowed; month-end date math |

## Technical Details

Namespace `Notary.Validity`, file-scoped namespaces, Allman bracing, positional records per coding standards. Pure functions taking `now` as a parameter so tests are deterministic. Verified with `dotnet build` and `dotnet test ./src/test/`.

---
*Plan approved at checkpoint. Execution follows.*
