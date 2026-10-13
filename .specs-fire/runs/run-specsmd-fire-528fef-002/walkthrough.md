---
run: run-specsmd-fire-528fef-002
work_item: validity-server-enforcement
intent: validity-period-dropdown
generated: 2026-10-13T00:55:00Z
mode: confirm
---

# Implementation Walkthrough: Server-side issuer validity enforcement

## Summary

Certificate and child-CA issuance now rejects any request whose validity is not contained within its issuing CA's. The rejection is a `Result` with status `Invalid` and a readable message, returned before any key is generated or anything is saved. Both Create pages show the message in a snackbar and stay on the page.

## Structure Overview

A pure check, `ValidityPolicy.CheckWithinIssuer`, lives in core next to the period policy. `CertificateService.IssueCertificateAsync` runs it right after loading the parent certificate and returns the failure early. `CertificateAuthorityService.CreateAsync` is the Result-returning creation path for CAs and propagates a failed issuance; the old `SaveAsync` override delegates to it. The two web pages call the Result-returning methods and display `result.Error`.

## Files Changed

### Created

| File | Purpose |
|------|---------|
| (none) | |

### Modified

| File | Changes |
|------|---------|
| `src/library/core/Validity/ValidityPolicy.cs` | Added `CheckWithinIssuer` |
| `src/library/core/Interface/Service/ICertificateService.cs` | `IssueCertificateAsync` returns `Task<Result<Certificate>>` |
| `src/library/core/Interface/Service/ICertificateAuthorityService.cs` | Added `CreateAsync` |
| `src/library/service/CertificateService.cs` | Issuer validity check before key generation; returns `Result<Certificate>` |
| `src/library/service/CertificateAuthorityService.cs` | `CreateAsync` holds the logic; `SaveAsync` delegates and throws on failure |
| `src/web/Pages/CreateCertificate.razor.cs` | Snackbar on failure, no navigation; injected `ISnackbar` |
| `src/web/Pages/CreateCertificateAuthority.razor.cs` | Calls `CreateAsync`; snackbar on failure; injected `ISnackbar` |
| `src/test/CertificateRevokeServiceTest.cs` | Helper split into `IssueAsync`/`TryIssueAsync`; 5 new tests |
| `src/test/CertificateServiceTest.cs` | Adapted to the `Result` return type |
| `src/test/CertificateAuthorityServiceTest.cs` | 2 new `CreateAsync` tests |
| `src/test/ValidityPolicyTest.cs` | 5 new `CheckWithinIssuer` tests |

## Key Implementation Details

### 1. The rule

Given the child's `notBefore`/`notAfter` and the issuer's: fail if `notAfter <= notBefore`, if `notAfter > issuerNotAfter`, or if `notBefore < issuerNotBefore`. Equal to the issuer's bounds passes. Self-signed roots have no parent, so the check is skipped.

### 2. Early return

The check runs before the key pair is generated and before `SaveAsync`, so a rejected request leaves no key or certificate behind. A missing parent remains an exception because it signals an unexpected state, not user input.

### 3. Interface change

`ICertificateService.IssueCertificateAsync` changed from `Task<Certificate>` to `Task<Result<Certificate>>`. The in-repo callers (CA service, Create Certificate page, tests) were updated; the whole solution builds.

## Security Considerations

| Concern | Approach |
|---------|----------|
| A child certificate valid after its issuer expires | Rejected server-side, so the API and any other client are covered, not only the UI |
| A child valid before its issuer began | Rejected |

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Error shape | `Result<Certificate>` from `IssueCertificateAsync`, new `CreateAsync` for CAs | Matches coding standards; approved at plan checkpoint |
| `SaveAsync` for CAs | Kept, delegates to `CreateAsync` and throws on failure | Preserves the `IEntityService` signature |
| Where the new service tests live | `CertificateRevokeServiceTest.cs` | Its in-memory fakes are private there |

## Deviations from Plan

None

## Dependencies Added

| Package | Why Needed |
|---------|------------|
| (none) | |

## How to Verify

1. **Run the tests**
   ```bash
   dotnet test ./src/test/
   ```
   Expected: 99 passed, 0 failed.

2. **Manual check in Visual Studio (UAT)**: create a root CA with 12 months, then try to create an intermediate CA with 48 months, then a leaf certificate longer than the issuer. Expected: a red snackbar with "The validity period cannot extend beyond the issuing CA's expiry (date)" and no navigation. The free-text field still exists until the dropdown items land, so this is the way to exercise it.

## Test Coverage

- Tests added: 12
- Coverage: not measured
- Status: passing

## Ready for Review

- [x] All acceptance criteria met
- [x] Tests passing
- [x] No critical issues
- [x] Documentation updated (if applicable)
- [x] Developer notes captured

## Developer Notes

- Anything else calling `CertificateAuthorityService.SaveAsync` for a new CA now gets a `NotaryException` on rejection rather than a Result; use `CreateAsync`.
- The dropdown items should avoid offering a period that ends within moments of the issuer's expiry, or the submit-time `now` could tip it over.

---
*Generated by specs.md - fabriqa.ai FIRE Flow Run run-specsmd-fire-528fef-002*
