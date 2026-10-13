# Code Review: ca-purpose-selector-ui

Run: run-specsmd-fire-528fef-008

## Scope

Created: `CaPurposeOption.cs`. Modified: `CaProfileCatalog.cs`, `CertificateAuthorityService.cs`, `CreateCertificateAuthorityViewModel.cs`, `CreateCertificateAuthority.razor`, `CreateCertificateAuthority.razor.cs`, `CaProfileCatalogTest.cs`.

## Checks

- Solution builds, 0 errors; 205 tests pass; `dotnet format` clean.
- The service refactor (private `ResolveProfile` and the inline path length check replaced by `CaProfileCatalog.Resolve` and `CheckIssuer`) was verified by the 18 real-certificate issuance tests passing unchanged.
- The page and the service now share one set of rules, so the selector cannot offer something the server would reject.

## Auto-fixes applied

None.

## Observations

- **Check order changed slightly**: `CheckIssuer` tests "parent can issue CAs at all" before the child-profile subset rule, so a CA under a path-length-0 parent reports the clearer "end-entity only" message even when the child would also be outside the parent's usages. No test relied on the old order.
- **Custom is partly unavailable for now**: under a restricted parent Custom is disabled (it must list explicit usages), and standalone it saves with no chosen usages, which the service issues with the previous broad set. `custom-ca-usages-ui` removes both limits. The page text says so.
- **Is Issuer default**: selecting any purpose other than Root turns Is Issuer on, because the Create Certificate page lists only CAs marked as issuers. Selecting Root turns it off and locks it. Changing purpose resets it, so a manual choice is overwritten if the purpose changes afterwards.
- **No default purpose**: the field starts empty and is required, so a purpose is a deliberate choice.
- **Not browser-tested**: `@bind-Value:after` on the MudBlazor select and the disabled-item rendering are standard patterns here but unverified in a running app.
- **Existing behavior**: the parent list shows every CA, including older ones labeled "Legacy". Older CAs remain usable as parents and keep working as before.
