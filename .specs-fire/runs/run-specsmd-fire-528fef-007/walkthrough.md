---
run: run-specsmd-fire-528fef-007
work_item: ca-cert-from-profile
intent: ca-purpose-profiles
generated: 2026-10-13T05:10:00Z
mode: validate
---

# Implementation Walkthrough: Issue CA certificates from the purpose profile

## Summary

`CertificateAuthorityService.CreateAsync` now builds a CA's certificate from its purpose profile: key usage, extended key usages and a basicConstraints path length. It also refuses to create a child CA that falls outside its parent's profile, or that sits under a parent which can only issue end-entity certificates. CAs created without a purpose behave exactly as before. Nothing in the UI supplies a purpose yet.

## Structure Overview

`CertificateRequest` gained an optional `PathLengthConstraint`, which `CertificateService` validates and passes down to the basicConstraints extension. `CertificateAuthorityService` resolves the new CA's profile and its parent's, runs the parent and path length checks first, then fills the request from the profile and issues it.

```
CreateAsync(CA)
  parent? -> CheckChild(parent profile, child profile)   -> Invalid (nothing saved)
          -> parent path length 0?                       -> Invalid (nothing saved)
  profile -> key usages, EKUs, pathLength = GetPathLength(purpose, hasParent)
  IssueCertificateAsync(request) -> basicConstraints(CA, pathLen) critical
```

## Files Changed

### Created

| File | Purpose |
|------|---------|
| `src/test/InMemoryFakes.cs` | In-memory repositories and key service shared by real-certificate tests, plus a CA repository fake |
| `src/test/CaPurposeIssuanceTest.cs` | 18 tests that issue real CA certificates by purpose and parse them |

### Modified

| File | Changes |
|------|---------|
| `src/library/core/Contract/CertificateRequest.cs` | Optional `PathLengthConstraint` (JSON `pathLen`) |
| `src/library/service/CertificateService.cs` | Validates and plumbs the path length into basicConstraints |
| `src/library/service/CertificateAuthorityService.cs` | Profile-driven request; parent, child and path length checks; legacy fallback |
| `src/test/CertificateRevokeServiceTest.cs` | Uses the shared fakes instead of private nested ones |

## Key Implementation Details

### 1. What goes into a CA certificate

| CA | Key usage | EKUs | Path length |
|---|---|---|---|
| Root | keyCertSign, cRLSign | none | none |
| Purpose CA, self-signed | keyCertSign, cRLSign | the purpose's | none |
| Purpose CA with a parent | keyCertSign, cRLSign | the purpose's | 0 |
| Custom with chosen usages | chosen + keyCertSign + cRLSign | chosen | none |
| No purpose, or Custom with nothing chosen | the previous broad five | the previous four | none |

basicConstraints is critical, so a conforming validator enforces the path length.

### 2. Order of checks

All checks run immediately after the parent is loaded and before the entity is changed or any key is generated, so a rejected request leaves nothing behind (a test counts saved CAs and certificates).

### 3. Legacy callers

A CA with no purpose keeps the old broad usages. Under a restricted parent it is rejected with "must list explicit usages", because it would otherwise escape the parent's restriction.

## Security Considerations

| Concern | Approach |
|---------|----------|
| Over-privileged CA certificates | Purpose CAs carry only keyCertSign and cRLSign |
| Widening down a chain | Child EKUs must be a subset of the parent's; checked before any key exists |
| Sub-CAs under an intermediate | Path length 0 in a critical extension, and rejected by Notary as well |
| Misuse of the path length field | Negative values, and values on non-CA requests, are rejected |

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Where profile logic lives | CA service maps profile to request; certificate service stays generic | Per the approved design |
| Legacy fallback | Old broad usages when no purpose | Existing callers keep working (confirmed at design review) |
| Test fakes | Moved to a shared file | New tests needed them; avoids two copies |

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
   Expected: 185 passed, 0 failed.

2. **Real-chain UAT (yours, as agreed at design review)**: the UI cannot select a purpose yet, so this is easiest after the selector item lands. Then: create a Root, a TLS/SSL intermediate under it, and issue a server certificate from the intermediate. Validate the chain on Windows (for example with `certutil -verify`) and ideally on Linux (`openssl verify -CAfile root.pem -untrusted intermediate.pem leaf.pem`). Use CAs created after this change, not older ones.

## Test Coverage

- Tests added: 18
- Coverage: not measured
- Status: passing (185)

## Ready for Review

- [x] All acceptance criteria met
- [x] Tests passing
- [x] No critical issues
- [x] Documentation updated (if applicable)
- [x] Developer notes captured

## Developer Notes

- Older CAs already carry four specific EKUs on their certificates (no e-mail protection), so strict validators may reject S/MIME leaves under them regardless of Notary's policy. Test purposes with newly created CAs.
- The tests parse certificates with BouncyCastle; they cannot show how Windows or Linux validators behave. That is the remaining risk named at design review.

---
*Generated by specs.md - fabriqa.ai FIRE Flow Run run-specsmd-fire-528fef-007*
