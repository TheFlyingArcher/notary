# Code Review: leaf-validity-dropdown

Run: run-specsmd-fire-528fef-004

## Scope

Modified: `CreateCertificate.razor`, `CreateCertificate.razor.cs`, `CreateCertificateViewModel.cs`.

## Checks

- Solution builds, 0 errors; 99 tests pass.
- Same structure as the Create CA page (options property, `RefreshValidity()`, `AddTo` on submit), so the two pages behave alike.
- The page was not run: no MongoDB and no login available here.

## Auto-fixes applied

None.

## Observations

- `RefreshValidity()` and the `ValidityOptions`/`AnyValidityAllowed`/`ValidityHelperText` trio are now duplicated in both Create pages, differing only in role and wording. A shared helper could remove the duplication; I left it because it would add a new abstraction across two Blazor pages for a small amount of code.
- `NotBefore` for a leaf is `DateTime.UtcNow` at submit, and a period ending at the issuer's exact expiry could be rejected by the server by seconds, since the dropdown was evaluated at an earlier instant. The server returns a clear snackbar error in that case. Not worth special handling.
- `OnCaListChange` in this page has no callers (dead code that predates this change). I left it alone.
- A self-signed certificate (no issuer) now gets the full leaf list with no cap, which matches the requirement.
