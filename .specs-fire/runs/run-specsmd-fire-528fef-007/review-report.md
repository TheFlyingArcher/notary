# Code Review: ca-cert-from-profile

Run: run-specsmd-fire-528fef-007

## Scope

Modified: `CertificateRequest.cs`, `CertificateService.cs`, `CertificateAuthorityService.cs`, `CertificateRevokeServiceTest.cs`. Created: `InMemoryFakes.cs`, `CaPurposeIssuanceTest.cs`.

## Checks

- Solution builds, 0 errors; 185 tests pass; `dotnet format` clean.
- Security-sensitive change reviewed for: ordering of checks (all run before any key is generated), what ends up in the signed certificate (read back by tests), and backward compatibility.
- A mutation check confirmed the path length test fails when the behavior is removed.

## Auto-fixes applied

None.

## Observations

- **Legacy CAs already restrict EKUs on the certificate.** A CA created before this change carries four EKUs: code signing, server auth, client auth and smart card login, but not e-mail protection. A strict validator would therefore already reject an S/MIME leaf under such a CA, even though Notary treats the CA as unrestricted. This is existing behavior and not changed here, but it matters for the real-chain UAT: use a CA created after this change to test purposes.
- **Legacy fallback is deliberate.** A missing purpose, or Custom with nothing chosen, keeps the old broad usages so existing API callers behave as before. The Create CA UI will always supply a purpose once the selector item lands.
- **A Custom CA's stored usages are what the user chose**, while the certificate also carries `keyCertSign` and `cRLSign`. The saved record does not include the two forced ones.
- **`SaveAsync` for CAs** still throws `NotaryException` on rejection, as established earlier. The new rejections flow through `CreateAsync` as `Result`.
- **Test fakes moved** to `InMemoryFakes.cs` without behavior change. The revoke tests use the shared copies.
- **Embedded NotBefore**: certificate generation sets NotBefore to the generation time and ignores the request's NotBefore. This is pre-existing and unrelated to this item, but it means the validity checks compare against the requested NotBefore, not the embedded one.
- **Path length semantics**: 0 on a purpose CA with a parent means it cannot issue sub-CAs. A self-signed purpose CA can have same-purpose children, which then get 0.
