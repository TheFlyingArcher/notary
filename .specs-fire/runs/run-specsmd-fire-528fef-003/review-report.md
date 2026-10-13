# Code Review: ca-validity-dropdown

Run: run-specsmd-fire-528fef-003

## Scope

Modified: `CreateCertificateAuthority.razor`, `CreateCertificateAuthority.razor.cs`, `CreateCertificateAuthorityViewModel.cs`.

## Checks

- Solution builds, 0 errors; 99 tests pass.
- Razor and view-model conform to existing patterns (injection, view model property style).
- The page was not run: no MongoDB and no NotaryAdmin login here.

## Auto-fixes applied

None.

## Observations

- `ValidityOptions` is recomputed on every access (several times per render) using `DateTime.UtcNow`. It is a handful of records, so cost is negligible, but the evaluation instant differs slightly between renders; the server-side check is the backstop.
- The parent-changed handler was already a fire-and-forget async lambda invoked from a property setter. I added `await InvokeAsync(StateHasChanged)` after `RefreshValidity()` so the dropdown updates once the parent loads. If the parent fetch throws (as the existing handler can for an invalid key size), that exception path is unchanged.
- If a parent has so little life left that no option fits, the select is disabled and the helper text says so; the Required check then blocks submission.
- The "(recommended)" tag and disabled reason are appended in the item text, so the closed select shows only the plain label via `ToStringFunc`.
