# Code Review: custom-ca-usages-ui

Run: run-specsmd-fire-528fef-011

## Scope

Modified: `KeyPurposeOids.cs`, `CaProfileCatalog.cs`, `CreateCertificateAuthorityViewModel.cs`, `CreateCertificateAuthority.razor`, `CreateCertificateAuthority.razor.cs`, `CaProfileCatalogTest.cs`, `CaPurposeIssuanceTest.cs`.

## Checks

- Library and test projects: 0 errors; web project compiled to a temporary folder: 0 errors; 302 tests pass; `dotnet format` clean.
- Mutation check confirmed the tests fail if Custom CAs fall back to the legacy broad usages.
- The server still enforces the rules the page shows (child within parent, explicit usages under a restricted parent), so the page is a convenience on top.

## Auto-fixes applied

None.

## Observations

- **Intentional behavior change for Custom.** The page always sends the two fixed key usages, so a deliberate Custom CA is distinguishable from a CA created with no purpose. A Custom CA with nothing extra chosen now has only certificate and CRL signing and no extended key usage, instead of the previous broad set. The broad set remains only for callers that send no purpose, which keeps API callers unchanged.
- **Duplicate list of extended key usages.** `KeyPurposeOids.Known` repeats the list kept in the Create Certificate view model. Two names differ slightly ("Smart Card Login" here, "SMART Card Login" there). I left the certificate form as it is, since its screen has just passed UAT; consolidating the two is a small follow-up.
- **Custom is offered under a restricted parent.** The explicit-usages requirement moved from "option disabled" to "field required" (page) and `CheckIssuer` (server), so the user can act on it instead of being blocked with no way forward.
- **Clearing on parent change.** Extended key usages that a newly chosen parent does not allow are dropped automatically, so a stale choice cannot be submitted.
- **Not browser-tested.** The multi-select bindings follow the same pattern as the Create Certificate form but are unverified in a running app.
