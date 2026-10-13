# Code Review: validity-period-policy

Run: run-specsmd-fire-528fef-001

## Scope

Created: `src/library/core/Validity/{CertificateRole,ValidityPeriod,ValidityOption,ValidityPolicy}.cs`, `src/test/ValidityPolicyTest.cs`. Nothing modified.

## Checks

- `dotnet format --verify-no-changes` on the new files: clean.
- Standards: file-scoped namespaces, Allman bracing, positional records, collection expressions, PascalCase/underscore-private-field naming: conforming.
- Full suite re-run after review: 87 passed, 0 failed.

## Auto-fixes applied

None needed.

## Observations (no action required)

- `GetPeriods`/`GetRecommended` throw `ArgumentOutOfRangeException` for an undefined enum value. That is a programming error, not a business failure, so it does not conflict with the Result-pattern guidance.
- `Evaluate` compares against the caller-supplied `now`. Callers must use the same instant when computing the submitted NotAfter, or an option shown as allowed could be a moment past the issuer's expiry. The server-side enforcement item (`validity-server-enforcement`) is the backstop.
- Leaf options of 30 and 90 days relate to the existing 12-month minimum check in `CreateCertificate.razor.cs`, which is removed by `leaf-validity-dropdown`.
- Recommended defaults are the draft values approved with the plan.
