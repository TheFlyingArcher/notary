---
run: run-specsmd-fire-528fef-005
work_item: ca-purpose-profile-catalog
intent: ca-purpose-profiles
generated: 2026-10-13T03:10:00Z
mode: validate
---

# Implementation Walkthrough: CA purpose profile catalog

## Summary

Added a pure library of CA purposes and their profiles in `Notary.Core`. It defines, for each of the seven purposes, what the CA certificate carries and what the certificates it issues may carry, plus the rules for path length and for whether a child CA fits inside its parent. Nothing uses it yet; the persistence, certificate issuance, enforcement and UI items build on it.

## Structure Overview

`CaProfileCatalog` holds seven immutable `CaProfile` records. Callers look up a profile with `Get(purpose)` (null means Custom), list them with `GetAll()` for the selector, build a Custom profile from user choices with `CreateCustom`, ask for a CA's path length with `GetPathLength`, and validate a parent and child pair with `CheckChild`. `KeyPurposeOids` gives named EKU constants and readable names for error messages.

## Files Changed

### Created

| File | Purpose |
|------|---------|
| `src/library/core/Profiles/CaPurpose.cs` | Purpose enum in UI order |
| `src/library/core/Profiles/KeyPurposeOids.cs` | EKU OID constants and `GetName` |
| `src/library/core/Profiles/CaProfile.cs` | Profile record with `RestrictsExtendedKeyUsages` |
| `src/library/core/Profiles/CaProfileCatalog.cs` | Catalog, Custom factory, path length, child check |
| `src/test/CaProfileCatalogTest.cs` | 55 test cases |

### Modified

| File | Changes |
|------|---------|
| (none) | |

## Domain Model

### Value Objects

| Value Object | Properties | Constraints |
|--------------|------------|-------------|
| `CaProfile` | Purpose, DisplayName, Description, IsDiscouraged, CaKeyUsages, CaExtendedKeyUsages, IssuesCaCertificates, IssuesLeafCertificates, RestrictsLeafUsages, LeafKeyUsages, LeafExtendedKeyUsages | Immutable positional record; every CA carries only keyCertSign + cRLSign |

## Key Implementation Details

### 1. Approved profiles

| Purpose | CA EKUs | Leaf key usages | Leaf EKUs |
|---|---|---|---|
| Root | none | n/a (issues CAs only) | n/a |
| TLS/SSL | Server Auth | Digital Signature, Key Encipherment | Server Auth |
| User login | Client Auth, Smart Card Login | Digital Signature, Key Encipherment | Client Auth, Smart Card Login |
| S/MIME | E-mail Protection | Digital Signature, Non Repudiation, Key Encipherment | E-mail Protection |
| IoT devices | Client Auth, Server Auth | Digital Signature, Key Encipherment | Client Auth, Server Auth |
| 802.1x/RADIUS | Client Auth, Server Auth | Digital Signature, Key Encipherment | Client Auth, Server Auth |
| Custom | user-chosen | user-chosen (unrestricted) | user-chosen (unrestricted) |

### 2. Child check

Root can never be a child. A parent with no EKU restriction (Root, or a Custom CA with none) accepts any child. Under a restricted parent the child must list EKUs and every one must be in the parent's set; the error names the offending usages.

### 3. Path length

None for Root, Custom and a missing purpose. For TLS, User login, S/MIME, IoT and 802.1x, 0 when the CA has a parent and none when it is self-signed.

## Security Considerations

| Concern | Approach |
|---------|----------|
| CA over-privilege | Every CA is limited to keyCertSign and cRLSign |
| Widening down a chain | A child's EKUs must be a subset of its parent's; Custom must be explicit under a restricted parent |
| Roots issuing end-entity certificates | `IssuesLeafCertificates` is false for Root; enforcement comes in `leaf-profile-enforcement` |

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Where Custom's chosen usages live | `CreateCustom` returns a Custom profile instance | The shared profile table stays immutable; a CA's own choices are carried per instance |
| Forced Custom usages | keyCertSign and cRLSign always added | A CA cannot sign or publish revocations without them |

## Deviations from Plan

None

## Dependencies Added

| Package | Why Needed |
|---------|------------|
| (none) | |

## How to Verify

1. **Run the new tests**
   ```bash
   dotnet test ./src/test/ --filter "FullyQualifiedName~CaProfileCatalogTest"
   ```
   Expected: 55 passed.

2. **Run the full suite**
   ```bash
   dotnet test ./src/test/
   ```
   Expected: 158 passed, 0 failed.

## Test Coverage

- Tests added: 55
- Coverage: not measured
- Status: passing

## Ready for Review

- [x] All acceptance criteria met
- [x] Tests passing
- [x] No critical issues
- [x] Documentation updated (if applicable)
- [x] Developer notes captured

## Developer Notes

- Leaf enforcement must check `IssuesLeafCertificates` before comparing usages. Root's leaf lists are empty and `RestrictsLeafUsages` is true, so comparing first would reject everything with a confusing message.
- The Smart Card Login profile is a draft until it is validated on a real Windows logon, as noted at design review.
- The catalog is not wired to anything. Visible behavior is unchanged until `ca-cert-from-profile` and the UI items land.

---
*Generated by specs.md - fabriqa.ai FIRE Flow Run run-specsmd-fire-528fef-005*
