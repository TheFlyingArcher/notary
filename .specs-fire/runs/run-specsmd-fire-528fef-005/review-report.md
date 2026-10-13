# Code Review: ca-purpose-profile-catalog

Run: run-specsmd-fire-528fef-005

## Scope

Created: `src/library/core/Profiles/{CaPurpose,KeyPurposeOids,CaProfile,CaProfileCatalog}.cs`, `src/test/CaProfileCatalogTest.cs`. Nothing modified.

## Checks

- Solution builds, 0 errors; 158 tests pass; `dotnet format --verify-no-changes` clean.
- Standards: file-scoped namespaces, Allman bracing, positional record, collection expressions, `Result` for expected failures, no exceptions for validation (only `ArgumentNullException` for null arguments).
- The profile data was compared line by line with the approved design table.

## Auto-fixes applied

One, made during implementation before the first build: removed a nonsense placeholder `return` in `CheckChild`.

## Observations

- `Get(purpose)` uses `First`, which throws if an enum value has no profile. A test (`GetAll_ReturnsSevenPurposesInUserOrder`) fails first if one is ever added without a profile.
- The Root profile has empty leaf lists and `RestrictsLeafUsages = true`. Combined with `IssuesLeafCertificates = false`, a consumer that checks the issues flag first never reaches the empty lists. Leaf enforcement must check `IssuesLeafCertificates` before comparing usages.
- Custom profiles built with `CreateCustom` leave leaf usages unrestricted, matching the design ("Custom leaves unrestricted").
- `CheckChild` returns an `Invalid` result when a parent cannot issue CA certificates, but every current profile can, so that branch has no test. It exists for safety if a profile later says otherwise.
- The path length rule is only a function here. Honoring it when issuing, including rejecting a CA under a pathLen-0 parent, is in `ca-cert-from-profile`.
