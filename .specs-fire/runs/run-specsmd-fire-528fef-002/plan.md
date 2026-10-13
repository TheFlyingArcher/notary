---
run: run-specsmd-fire-528fef-002
work_item: validity-server-enforcement
intent: validity-period-dropdown
mode: confirm
checkpoint: plan
approved_at: pending
---

# Implementation Plan: Server-side issuer validity enforcement

## Approach

Reject a certificate or child CA whose validity is not contained within its issuer's, in the service layer, before any key is generated or anything is saved. The failure is a `Result` (status `Invalid`), not an exception, and the UI shows `result.Error` in a snackbar, the same pattern `RevokeCertificateDialog` already uses.

1. **Pure check in core**: add `ValidityPolicy.CheckWithinIssuer(notBefore, notAfter, issuerNotBefore, issuerNotAfter)` returning `Result`. It fails with `Invalid` and a readable message when `notAfter > issuerNotAfter` or `notBefore < issuerNotBefore`; equal values pass. Also fail when `notAfter <= notBefore`.
2. **`CertificateService.IssueCertificateAsync`**: return `Task<Result<Certificate>>`. After loading the parent (which already happens), run the check and return `Result<Certificate>.Fail(ResultStatus.Invalid, ...)` before key generation. Self-signed (no parent) skips the check. A missing parent stays an exception (unexpected state), as today.
3. **`CertificateAuthorityService`**: add `CreateAsync(CertificateAuthority, string)` returning `Task<Result<CertificateAuthority>>`, containing the current `SaveAsync` logic and propagating a failed issuance. The existing `SaveAsync` override delegates to it and throws `NotaryException` on failure, so non-UI callers keep their signature. `ICertificateAuthorityService` gains `CreateAsync`.
4. **Callers**: `CreateCertificate.razor.cs` and `CreateCertificateAuthority.razor.cs` handle the failed result with a snackbar and stay on the page (no navigation, loading flag cleared). `CertificateAuthorityService` unwraps the issuance result. Tests that call `IssueCertificateAsync` read `.Value`.

### Decision for you

The issue wants a clear signal and the standards want `Result`, but `IssueCertificateAsync` currently returns `Certificate` and is part of `ICertificateService`, so this is a signature change on a public interface (3 callers plus tests). The alternative is leaving the signature alone and throwing `NotaryException`, which contradicts the standards for expected validation failures. I recommend the signature change above. Say so at approval if you prefer otherwise.

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/core/Validity/ValidityPolicy.cs` | Add `CheckWithinIssuer` |
| `src/library/core/Interface/Service/ICertificateService.cs` | `IssueCertificateAsync` returns `Task<Result<Certificate>>` |
| `src/library/core/Interface/Service/ICertificateAuthorityService.cs` | Add `CreateAsync` returning `Task<Result<CertificateAuthority>>` |
| `src/library/service/CertificateService.cs` | Issuer validity check before key generation; Result return |
| `src/library/service/CertificateAuthorityService.cs` | `CreateAsync`; `SaveAsync` delegates and throws on failure |
| `src/web/Pages/CreateCertificate.razor.cs` | Handle failed result with snackbar |
| `src/web/Pages/CreateCertificateAuthority.razor.cs` | Call `CreateAsync`, handle failed result with snackbar |
| `src/test/CertificateRevokeServiceTest.cs`, `src/test/CertificateServiceTest.cs` | Adapt to the new return type |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/ValidityPolicyTest.cs` | `CheckWithinIssuer`: child outlives issuer, exact boundary passes, starts before issuer, end before start |
| `src/test/CertificateServiceTest.cs` | Leaf outliving its issuer returns `Invalid` and generates/saves nothing; leaf within issuer succeeds; self-signed root unaffected |
| `src/test/CertificateAuthorityServiceTest.cs` | Child CA outliving its parent returns `Invalid`; valid child succeeds |

## Technical Details

The service tests need real issuance (the existing revoke tests already generate real certificates in-test), so the new service tests build a real parent certificate and request children with different NotAfter values. Verified with `dotnet build` and `dotnet test ./src/test/`; the web pages are compiled by the build and the UI behavior is covered by UAT in Visual Studio.

---
*Plan approved at checkpoint. Execution follows.*
