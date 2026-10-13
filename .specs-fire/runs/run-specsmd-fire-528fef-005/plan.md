---
run: run-specsmd-fire-528fef-005
work_item: ca-purpose-profile-catalog
intent: ca-purpose-profiles
mode: validate
checkpoint: plan
approved_at: pending
---

# Implementation Plan: CA purpose profile catalog

## Approach

Implements the approved design (`.specs-fire/intents/ca-purpose-profiles/work-items/ca-purpose-profile-catalog-design.md`) as a pure library in `Notary.Core`, namespace `Notary.Profiles`. Nothing consumes it yet.

- **`CaPurpose`** enum, in UI order: `Root, TlsSsl, UserLogin, Smime, IoT, Dot1xRadius, Custom`.
- **`KeyPurposeOids`** static class: named constants for the EKU OIDs used (Server Auth, Client Auth, E-mail Protection, Smart Card Login) and `GetName(oid)` for readable error messages.
- **`CaProfile`** positional record: `Purpose, DisplayName, Description, IsDiscouraged, CaKeyUsages, CaExtendedKeyUsages, IssuesCaCertificates, IssuesLeafCertificates, RestrictsLeafUsages, LeafKeyUsages, LeafExtendedKeyUsages`, plus a computed `RestrictsExtendedKeyUsages` (true when the CA certificate carries EKUs). Lists are `IReadOnlyList`.
- **`CaProfileCatalog`** static class:
  - `Get(CaPurpose? purpose)` returns the profile; null resolves to Custom.
  - `GetAll()` returns the seven in UI order for the selector.
  - `CreateCustom(keyUsages, extendedKeyUsages)` returns a Custom profile with the chosen usages. `keyCertSign` and `cRLSign` are always added, and duplicates are removed. Leaves of a Custom CA stay unrestricted.
  - `GetPathLength(CaPurpose? purpose, bool hasParent)` returns null for Root, Custom and null purposes and for a self-signed purpose CA; returns 0 for a purpose CA that has a parent.
  - `CheckChild(parent, child)` returns a `Result`:
    - Root as a child is `Invalid`.
    - A parent with no EKU restriction (Root, Custom with none) accepts anything.
    - Otherwise the child's CA EKUs must be non-empty and a subset of the parent's. Errors name the offending usages.
    - A Custom child with no EKUs under a restricted parent is `Invalid` ("must list explicit usages").

Profile data follows the approved table: every CA is `keyCertSign + cRLSign`; Root has no EKU and `IssuesLeafCertificates = false`; TLS/SSL is Server Auth only; User login is Client Auth + Smart Card Login; S/MIME is E-mail Protection; IoT and 802.1x are Client Auth + Server Auth; the leaf key usages and EKUs match the table.

## Files to Create

| File | Purpose |
|------|---------|
| `src/library/core/Profiles/CaPurpose.cs` | Purpose enum |
| `src/library/core/Profiles/KeyPurposeOids.cs` | Named EKU OIDs and readable names |
| `src/library/core/Profiles/CaProfile.cs` | Profile record |
| `src/library/core/Profiles/CaProfileCatalog.cs` | Catalog, custom factory, path length, child check |
| `src/test/CaProfileCatalogTest.cs` | NUnit tests |

## Files to Modify

| File | Changes |
|------|---------|
| (none) | |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/CaProfileCatalogTest.cs` | Every approved profile's CA usages, EKUs and leaf sets; seven purposes in UI order; null resolves to Custom; Root never issues leaves and purposes do; `CreateCustom` forces and de-duplicates `keyCertSign`/`cRLSign`; the path length matrix; the child matrix (Root as child rejected, TLS under Root and under TLS allowed, S/MIME under TLS rejected, TLS under IoT allowed, User login under IoT rejected, Custom with no EKUs under a restricted parent rejected, Custom within the parent's set allowed, Custom outside rejected, anything under an unrestricted Custom allowed) |

## Technical Details

Positional records, file-scoped namespaces, Allman bracing, `Result` for expected failures, no exceptions. Verified with `dotnet build src/Notary.sln`, `dotnet test ./src/test/`, and `dotnet format --verify-no-changes` on the new files.

## Based on Design Doc

Reference: `.specs-fire/intents/ca-purpose-profiles/work-items/ca-purpose-profile-catalog-design.md`

---
*Plan approved at checkpoint. Execution follows.*
