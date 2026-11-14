# Implementation Plan: Bump AutoMapper to 16.3.0

Run: run-dependabot-security-alert-95cd80-001 (autopilot, single)

## Approach

Change the `AutoMapper` PackageReference in `Notary.Data.csproj` from 16.1.1 to 16.3.0, restore, build and run the full test suite. Confirm no other project pins AutoMapper.

## Files to Create

None.

## Files to Modify

- `src/library/data/Notary.Data.csproj` - AutoMapper 16.1.1 to 16.3.0

## Tests

No new tests (bump only, per intent scope). The existing suite, including `CaPurposePersistenceTest` and the repository tests, covers the mapping profile.
