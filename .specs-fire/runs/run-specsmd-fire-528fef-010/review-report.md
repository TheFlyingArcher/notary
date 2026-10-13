# Code Review: leaf-profile-ui

Run: run-specsmd-fire-528fef-010

## Scope

Modified: `CaProfile.cs`, `CreateCertificateViewModel.cs`, `CreateCertificate.razor`, `CreateCertificate.razor.cs`, `CaProfileCatalogTest.cs`.

## Checks

- Library, tests: 0 errors; web project compiled to a temporary folder: 0 errors; 290 tests pass; `dotnet format` clean.
- The page and the server use the same rule (`Resolve` and the profile), so the UI cannot offer usages the server then rejects.
- The server-side check from run 009 remains the backstop for any client other than this page.

## Auto-fixes applied

None beyond the nullable declaration noted in the test report.

## Observations

- **Locked values live in the view model.** When a purpose CA is chosen, its leaf usages are copied into the view model's selected usages, so the existing submit path is unchanged. Leaving the CA restores the defaults, so locked values never reach an editable form.
- **Defaults are shared.** The view model's constructor and `ResetUsages()` now use one definition of the defaults (Server and Client Authentication, no key usage chosen), removing a duplicate.
- **Existing page warnings remain.** The page has several existing nullable and obsolete-method warnings (for example `MudForm.Validate`); this change adds none apart from one I fixed.
- **Root.** Root is filtered out of the issuer list. The page also shows a warning if a CA that cannot issue leaves is ever selected, for example one reached by another route.
- **Name lists.** The usage note uses the view model's existing name lists and falls back to the catalog's names, so every usage in a profile has a readable label.
- **Not browser-tested.** The conditional rendering (pickers removed, note shown) is standard Blazor but unverified in a running app.
