---
run: run-specsmd-fire-528fef-009
work_item: leaf-profile-enforcement
intent: ca-purpose-profiles
generated: 2026-10-13T08:10:00Z
mode: confirm
---

# Implementation Walkthrough: Enforce issuing CA profile on leaf certificates

## Summary

The server now refuses a leaf certificate whose usages fall outside the issuing CA's profile. A Root CA never issues a leaf at all. Self-signed certificates and CAs without a purpose behave exactly as before. The rule is enforced in the certificate service, before any key is generated, so it holds regardless of what the UI sends.

## Structure Overview

`CertificateService.IssueCertificateAsync` already loads the issuing certificate. For a non-CA request it now also finds the CA record that owns that certificate (by certificate slug), resolves the CA's profile, and asks `CaProfileCatalog.CheckLeaf` whether the requested key usages and extended key usages are allowed.

```
IssueCertificateAsync(leaf request)
  parent certificate loaded
  CA record for that certificate? (GetByCertificateSlugAsync)
     none -> unrestricted
     found -> Resolve(profile) -> CheckLeaf(key usages, EKUs)
                                     Invalid -> return (nothing generated or saved)
  generate key and certificate
```

## Files Changed

### Created

| File | Purpose |
|------|---------|
| (none) | |

### Modified

| File | Changes |
|------|---------|
| `src/library/core/Profiles/CaProfileCatalog.cs` | Added `CheckLeaf` |
| `src/library/core/Interface/Repository/ICertificateAuthorityRepository.cs` | Added `GetByCertificateSlugAsync` |
| `src/library/data/Repository/CertificateAuthorityRepository.cs` | Implemented it with the existing `cert_slug` index |
| `src/library/service/CertificateService.cs` | New constructor dependency; leaf check before key generation |
| `src/test/InMemoryFakes.cs` | CA repository fake supports the new query |
| `src/test/CaProfileCatalogTest.cs` | 11 `CheckLeaf` tests |
| `src/test/CaPurposeIssuanceTest.cs` | 16 real-certificate cases |
| `src/test/CertificateRevokeServiceTest.cs` | Pass the CA repository to `CertificateService` |

## Key Implementation Details

### 1. What is allowed

| Issuing CA | Leaf key usages | Leaf extended key usages |
|---|---|---|
| Root | never issues leaves | never issues leaves |
| Named purpose | at least one, all within the profile | at least one, all within the profile |
| Custom with chosen EKUs | free | within the CA certificate's own EKUs |
| No purpose, or no CA record | free | free |
| Self-signed (no issuer) | free | free |

### 2. Why EKUs are required for a purpose CA

A leaf with no EKU is valid for any purpose as far as the leaf is concerned, which would defeat the point of a purpose CA. The server requires at least one, and the UI work item will set them for the user.

### 3. Error messages

Rejections name the offending usage and the CA's purpose, for example "Client Authentication not permitted by the TLS/SSL CA." and, for Root, "A Root CA never issues certificates directly. Create an intermediate CA and issue from that."

## Security Considerations

| Concern | Approach |
|---------|----------|
| A purpose CA signing certificates outside its purpose | Rejected server-side before any key exists, whatever the client sends |
| Root signing end-entity certificates | Rejected |
| Combined key usage values hiding extra usages | Flags are decomposed before checking |

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Where the CA is found | Repository query by certificate slug | The issuing certificate does not know its CA record; the `cert_slug` index already exists |
| No CA record | Unrestricted | Preserves behavior for existing and directly issued certificates |
| CAs are not checked here | Only non-CA requests | CA creation has its own checks |

## Deviations from Plan

None.

## Dependencies Added

| Package | Why Needed |
|---------|------------|
| (none) | |

## How to Verify

1. **Run the tests**
   ```bash
   dotnet test ./src/test/
   ```
   Expected: 282 passed, 0 failed.

2. **UAT (Visual Studio)**: stop and restart the app so it picks up the change. On Create Certificate, pick the TLS/SSL CA, tick Client Authentication as well as Server Authentication and submit. Expected: a red message "Client Authentication not permitted by the TLS/SSL CA." and no certificate created. Then try a Root CA as issuer if one is offered (it normally is not, because Root is not an issuer). Self-signed certificates keep all options.

## Test Coverage

- Tests added: 27
- Coverage: not measured
- Status: passing (282)

## Ready for Review

- [x] All acceptance criteria met
- [x] Tests passing
- [x] No critical issues
- [x] Documentation updated (if applicable)
- [x] Developer notes captured

## Developer Notes

- The Windows negative test (a leaf with Server and Client Authentication from a server-only CA) can no longer be created through Notary. The files from the UAT remain valid evidence of what Windows did.
- The Create Certificate page still shows the usage pickers for purpose CAs. A user can pick usages and then gets a clear error. `leaf-profile-ui` hides them and presets the CA's usages.

---
*Generated by specs.md - fabriqa.ai FIRE Flow Run run-specsmd-fire-528fef-009*
