# Code Review: validity-server-enforcement

Run: run-specsmd-fire-528fef-002

## Scope

Modified: `ValidityPolicy.cs`, `ICertificateService.cs`, `ICertificateAuthorityService.cs`, `CertificateService.cs`, `CertificateAuthorityService.cs`, `CreateCertificate.razor.cs`, `CreateCertificateAuthority.razor.cs`, and three test files.

## Checks

- Full solution builds (`dotnet build src/Notary.sln`): 0 errors.
- Standards: expected validation failure is a `Result`, not an exception; the new check runs before key generation, so a rejected request generates and saves nothing.
- Full suite: 99 passed, 0 failed.

## Auto-fixes applied

None.

## Observations

- **Behavior to know about**: `CertificateAuthorityService.SaveAsync` now delegates to `CreateAsync` and throws `NotaryException` if issuance is rejected. Only the Create CA page called it, and that page now calls `CreateAsync`. Anyone else calling `SaveAsync` for a CA gets an exception instead of a Result for this one case.
- **Clock skew**: the NotBefore check has no tolerance. The UI sets NotBefore to the current time, which is later than any existing issuer's start, so it passes. An API client that backdates NotBefore earlier than its issuer's start is rejected, which is intended.
- **Race with the dropdown**: the dropdown items (next work items) evaluate against `now` at page load while the submit uses a later `now`. Because the server compares against the issuer's NotAfter, a period ending exactly at the issuer's expiry could be rejected by a few seconds. This is a UX edge to consider in the dropdown items (for example by offering the issuer's expiry or rounding), not a defect here.
- The new service tests sit in `CertificateRevokeServiceTest.cs` because its private in-memory fakes are defined there. Extracting them to a shared test helper would be a reasonable later cleanup.
