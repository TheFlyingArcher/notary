# Code Review: ca-purpose-persistence

Run: run-specsmd-fire-528fef-006

## Scope

Modified: `CertificateAuthority.cs` (contract), `CertificateAuthorityModel.cs`, `ModelMapProfile.cs`. Created: `CaPurposePersistenceTest.cs`.

## Checks

- Solution builds, 0 errors; 167 tests pass; `dotnet format` clean.
- All new properties are nullable and optional, so existing API callers and stored documents are unaffected.
- Purpose stored as a string in Mongo and by name in JSON, so reordering the enum cannot change what existing data means.

## Auto-fixes applied

None.

## Observations

- **Mapping rule**: The extra `AfterMap` in `ModelMapProfile` is scoped to the CA map and the two Custom collections only. It does not change how other models map their collections.
- **Null vs empty**: A Custom CA with no chosen usages and a legacy CA now both read back with null lists. Consumers should treat null and empty the same way (none chosen).
- **No live database**: BSON behavior was tested in memory only. The first real write and read of a CA with a purpose happens in UAT.
- **JSON converter**: `StringEnumConverter` is applied to the nullable purpose only. The existing enums on this contract still serialize as numbers, which this change leaves alone.
- **No index** was added; nothing queries by purpose.
