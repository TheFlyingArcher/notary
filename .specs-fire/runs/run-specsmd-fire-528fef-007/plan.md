---
run: run-specsmd-fire-528fef-007
work_item: ca-cert-from-profile
intent: ca-purpose-profiles
mode: validate
checkpoint: plan
approved_at: pending
---

# Implementation Plan: Issue CA certificates from the purpose profile

## Approach

Implements the approved design (`.specs-fire/intents/ca-purpose-profiles/work-items/ca-cert-from-profile-design.md`). Nothing in the UI changes: new CAs get a purpose only once the selector item lands, but the service honors a purpose as soon as one is supplied.

**1. Path length plumbing**
- `CertificateRequest` gains optional `PathLengthConstraint` (`int?`, JSON `pathLen`, null means no constraint).
- `CertificateService.GenerateCertificate` takes it and `AddBasicConstraints(gen, isCA, pathLength)` emits `new BasicConstraints(pathLength)` for a CA that has one, otherwise the existing `BasicConstraints(isCA)`. The extension stays critical.
- `IssueCertificateAsync` rejects a negative path length, or one on a non-CA request, with an `Invalid` `Result` before any key is generated.

**2. Profile-driven CA requests in `CertificateAuthorityService.CreateAsync`**
- Resolve the new CA's profile from `entity.Purpose`:
  - A named purpose uses its catalog profile.
  - Custom with chosen usages uses `CaProfileCatalog.CreateCustom`, which adds `keyCertSign` and `cRLSign`.
  - A missing purpose, or Custom with no usages, uses **today's broad usage set unchanged** (the five key usages and four EKUs now hardcoded), so existing callers behave as before.
- Key usages, EKUs and `PathLengthConstraint` (`GetPathLength(purpose, hasParent)`) come from that profile.
- The CA entity's `Purpose` is saved as given; a null purpose is not rewritten.

**3. Checks before any key is generated** (each an `Invalid` `Result`, nothing saved):
- Resolve the parent CA's profile the same way and run `CaProfileCatalog.CheckChild(parent, child)`. This also rejects a Root with a parent.
- Reject a CA under a parent whose own path length is 0 (a purpose CA that itself has a parent), with a message that it can only issue end-entity certificates.

Root keeps `keyCertSign` + `cRLSign` with no EKU and no path length, per the approved profile.

**4. Test infrastructure.** The in-memory fakes used by `CertificateRevokeServiceTest` are private nested classes, so new CA tests cannot reuse them. I will move them unchanged to a shared `src/test/InMemoryFakes.cs` (internal) and add a CA repository fake; the revoke tests then use the shared copies. This is the cleanup I flagged earlier and keeps one set of fakes.

## Files to Create

| File | Purpose |
|------|---------|
| `src/test/InMemoryFakes.cs` | Shared fakes moved out of the revoke test, plus a CA repository fake |
| `src/test/CaPurposeIssuanceTest.cs` | Real-certificate tests for CA issuance by purpose |

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/core/Contract/CertificateRequest.cs` | Add optional `PathLengthConstraint` |
| `src/library/service/CertificateService.cs` | Plumb path length into `GenerateCertificate` and `AddBasicConstraints`; validate it |
| `src/library/service/CertificateAuthorityService.cs` | Profile-driven request; parent, child and path length checks |
| `src/test/CertificateRevokeServiceTest.cs` | Use the shared fakes instead of private nested ones |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/CaPurposeIssuanceTest.cs` | Issues a real CA certificate per purpose and parses it, asserting key usage (`keyCertSign` + `cRLSign` only), EKUs and basicConstraints (CA flag, critical, path length). Root: no EKU and no path length. Purpose CA with a parent: pathLen 0; self-signed: none. Custom: chosen usages plus the two forced ones. Legacy (null purpose): the old broad set and no path length. Rejections, each leaving nothing saved: Root with a parent, S/MIME under TLS, Custom with no usages under a restricted parent, a CA under a pathLen-0 parent. A full chain Root → TLS intermediate → server leaf is issued and its certificates parse. Negative path length and a path length on a non-CA are rejected. |

## Technical Details

Security-sensitive: this changes what is put into signed CA certificates. Unit tests parse the issued certificates with BouncyCastle, but cannot prove how Windows or Linux validators treat the chain. Per the design review, the real-chain UAT (root, intermediate, leaf, on Windows and ideally Linux) is yours before relying on any purpose. Only newly created CAs are affected; existing CA certificates are not reissued. Verified with `dotnet build src/Notary.sln`, `dotnet test ./src/test/` and `dotnet format --verify-no-changes`.

## Based on Design Doc

Reference: `.specs-fire/intents/ca-purpose-profiles/work-items/ca-cert-from-profile-design.md`

---
*Plan approved at checkpoint. Execution follows.*
