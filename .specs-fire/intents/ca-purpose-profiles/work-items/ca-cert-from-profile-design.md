---
work_item: ca-cert-from-profile
intent: ca-purpose-profiles
created: 2026-10-13T00:00:00Z
mode: validate
checkpoint_1: approved
---

# Design: Issue CA certificates from the purpose profile

## Summary

`CertificateAuthorityService.CreateAsync` builds the CA certificate from the CA's purpose profile instead of today's hardcoded broad usages. It adds the profile's EKUs and a path length constraint, and rejects a child CA outside its parent's profile before any key is generated.

## Scope

**In Scope:**
- Profile-driven key usages, EKUs and path length on CA certificates
- Optional `PathLengthConstraint` on `CertificateRequest` and in basicConstraints
- Child-within-parent and parent-pathLen checks before key generation
- Real-certificate tests

**Out of Scope:**
- Persisting the purpose (ca-purpose-persistence)
- Leaf enforcement (leaf-profile-enforcement)
- Any UI
- Reissuing existing CA certificates

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Where profile logic lives | `CertificateAuthorityService` maps the profile to the request; `CertificateService` stays generic | Certificate generation does not need to know about purposes |
| Path length plumbing | `CertificateRequest` gains optional `PathLengthConstraint` (null means none); `AddBasicConstraints` takes it | Small, additive contract change; existing API callers unaffected |
| Path length values | Root: none. Self-signed purpose CA: none. Purpose CA with a parent: 0. Custom: none | Purpose CAs with a parent issue leaves only; Custom is for advanced users and is unconstrained |
| Child under a pathLen-0 parent | Rejected with an Invalid Result | A purpose intermediate cannot have CA children, matching its issued path length |
| Custom key usages | User's choices, with keyCertSign and cRLSign always added | A CA cannot sign without keyCertSign; revocation validates cRLSign |
| EKU extension criticality | Non-critical | A critical EKU on a CA breaks some validators; matches the existing extension |
| Custom with no explicit usages | Falls back to today's broad usage set | Keeps API callers and any path without a purpose working as before (confirmed by the user) |
| Child CA check timing | Before key generation, using the parent's resolved profile | Matches the validity and leaf checks; a rejected request leaves nothing behind |
| Existing CA certificates | Not reissued | They stay as they are; their CAs are Custom (unrestricted) |

## Data Models Affected

### Modifies
- **CertificateRequest**: optional `PathLengthConstraint` (int?) — carries the path length to certificate generation

## Technical Approach

### Architecture

```
CreateAsync(CA entity)
  1. resolve purpose (null -> Custom)
  2. load parent CA, resolve its profile
  3. CaProfileCatalog.CheckChild(parent, child)       -> Invalid, no key
  4. parent pathLen 0 and child is a CA                -> Invalid, no key
  5. build CertificateRequest from profile:
       key usages = CaKeyUsages (Custom: chosen + KeyCertSign + CrlSign)
       EKUs       = CaExtendedKeyUsages
       PathLengthConstraint = GetPathLength(purpose, hasParent)
  6. CertificateService.IssueCertificateAsync(request)
       -> AddBasicConstraints(isCA, pathLen)
```

## Affected Files

| File | Action | Purpose |
|------|--------|---------|
| `src/library/core/Contract/CertificateRequest.cs` | modify | Optional PathLengthConstraint |
| `src/library/service/CertificateService.cs` | modify | AddBasicConstraints and its call |
| `src/library/service/CertificateAuthorityService.cs` | modify | Profile-driven request, child checks |
| `src/test/CertificateRevokeServiceTest.cs` | modify | Real-certificate tests |
| `src/test/CertificateAuthorityServiceTest.cs` | modify | Rejection cases |

## Security Considerations

- **CA usages**: keyCertSign and cRLSign only (replaces the broad set).
- **No widening**: the child-within-parent check runs before any key is created.
- **Path length**: emitted in a critical basicConstraints extension, so a conforming validator enforces it.

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Wrong EKU or path length fails chain validation on a client | High | Tests parse the issued certificates. The user will run a real-chain UAT (root, intermediate, leaf) on Windows and ideally Linux before relying on any purpose. Only newly created CAs are affected. |
| pathLen 0 blocks a later extra tier | Medium | Selector UI explains it; Custom has no limit |
| CertificateRequest JSON contract change | Low | Optional field, null means none |
| A reviewer expects Root to keep broad usages | Low | Documented in the walkthrough |

## Implementation Checklist

- [ ] Add PathLengthConstraint to CertificateRequest
- [ ] Plumb it into AddBasicConstraints
- [ ] Build the CA request from the profile in CreateAsync, with the Custom fallback
- [ ] Run the parent, child and pathLen checks before key generation
- [ ] Real-certificate tests per purpose, plus the rejection cases

---
*Generated by specs.md - fabriqa.ai FIRE Flow | Checkpoint 1 approved: 2026-10-13T00:00:00Z*
