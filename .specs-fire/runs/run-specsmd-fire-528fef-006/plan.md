---
run: run-specsmd-fire-528fef-006
work_item: ca-purpose-persistence
intent: ca-purpose-profiles
mode: confirm
checkpoint: plan
approved_at: pending
---

# Implementation Plan: Persist CA purpose

## Approach

Add the purpose to the CA record end to end (contract, Mongo model, mapping), plus the user's chosen usages for Custom. Nothing sets or reads them yet; the next items do.

- **Contract** (`CertificateAuthority`): three new properties, all nullable so legacy CAs and existing API callers are unaffected.
  - `Purpose` (`CaPurpose?`, JSON `purpose`)
  - `CustomKeyUsages` (`List<CertificateKeyUsage>`, JSON `customKu`), used only when the purpose is Custom
  - `CustomExtendedKeyUsages` (`List<string>`, JSON `customEku`), used only when the purpose is Custom
- **Mongo model** (`CertificateAuthorityModel`): elements `purpose`, `custom_key_usages`, `custom_ex_key_usages`, all `BsonIgnoreIfNull` so nothing is written when unset. The purpose is stored as a string (`[BsonRepresentation(BsonType.String)]`), not an integer, so reordering the enum later can never silently change what a stored CA means. Key usages are stored as names, matching how `CertificateModel` stores its usages.
- **Mapping**: the existing AutoMapper `CreateMap<CertificateAuthority, CertificateAuthorityModel>().ReverseMap()` picks the new members up by name (enum lists map to and from strings), so no profile change is expected. A test proves it.
- **Legacy**: a stored CA without these elements loads with a null purpose, which `CaProfileCatalog.Get(null)` already resolves to Custom. No migration, and no data is rewritten.
- **JSON**: the purpose uses Newtonsoft's `StringEnumConverter`, so the API shows `"purpose": "TlsSsl"` rather than a number, for the same enum-reordering reason.

## Files to Create

| File | Purpose |
|------|---------|
| `src/test/CaPurposePersistenceTest.cs` | Mapping and BSON round-trip tests |

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/core/Contract/CertificateAuthority.cs` | Add `Purpose`, `CustomKeyUsages`, `CustomExtendedKeyUsages` |
| `src/library/data/Model/CertificateAuthorityModel.cs` | Add the three Mongo elements |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/CaPurposePersistenceTest.cs` | Contract to model to contract round trip for purpose and Custom usages; null purpose stays null; a stored document missing the new elements deserializes and resolves to Custom; the purpose is written as a string in BSON; unset values write no elements; JSON shows the purpose by name |

## Technical Details

No index is added: nothing queries by purpose. The Create CA page and the CA service are untouched in this item, so behavior is unchanged; new CAs will still be saved with a null purpose until `ca-cert-from-profile` and the selector UI set it. Verified with `dotnet build src/Notary.sln`, `dotnet test ./src/test/` and `dotnet format --verify-no-changes`.

---
*Plan approved at checkpoint. Execution follows.*
