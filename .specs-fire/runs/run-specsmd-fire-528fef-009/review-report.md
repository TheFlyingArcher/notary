# Code Review: leaf-profile-enforcement

Run: run-specsmd-fire-528fef-009

## Scope

Modified: `ICertificateAuthorityRepository.cs`, `CertificateAuthorityRepository.cs`, `CaProfileCatalog.cs`, `CertificateService.cs`, `InMemoryFakes.cs`, `CaProfileCatalogTest.cs`, `CaPurposeIssuanceTest.cs`, `CertificateRevokeServiceTest.cs`.

## Checks

- Library and test projects build, 0 errors; 282 tests pass; `dotnet format` clean.
- The check runs before key generation, so a rejected request generates and saves nothing (a test counts saved certificates).
- A mutation check showed the tests fail when the service check is removed.
- DI: `CertificateService` and `ICertificateAuthorityRepository` are both per-lifetime-scope, so there is no captive dependency.

## Auto-fixes applied

None.

## Observations

- **Key usage flags are decomposed.** The request's key usage flags are OR-ed together and split back into individual usages before checking, so a caller that sends a combined value (for example 160) is checked the same as one that sends the usages separately.
- **No CA record means unrestricted.** A certificate with no CA record behind it, or a legacy CA with no purpose, is not restricted. That is the agreed behavior for existing data, but it means an old CA whose certificate carries its four legacy EKUs is still not constrained by Notary; strict validators may restrict it anyway.
- **Server and UI are separate.** This run makes the server refuse off-purpose leaves. The Create Certificate page still shows the usage pickers for purpose CAs until `leaf-profile-ui`, so a user can still pick usages and then receive a clear error message. That is the next work item.
- **Constructor change.** `CertificateService` now takes the CA repository. The only direct constructions are in tests, and they are updated.
- **CA certificates are not checked here.** Requests for CA certificates skip the leaf rule; CA creation has its own checks (child within parent, path length).
- **Web project not built.** `Notary.Web` was running from Visual Studio and locked its output; this run changed no web code, and the web project compiled in earlier runs.
