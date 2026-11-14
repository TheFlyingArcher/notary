---
id: bump-automapper
title: Bump AutoMapper to 16.3.0
intent: upgrade-automapper-security
complexity: low
mode: autopilot
status: completed
depends_on: []
created: 2026-11-14T00:00:00Z
run_id: run-dependabot-security-alert-95cd80-001
completed_at: 2026-11-14T16:40:12.745Z
---

# Work Item: Bump AutoMapper to 16.3.0

## Description

Change the AutoMapper package reference in `Notary.Data.csproj` from 16.1.1 to 16.3.0 and confirm nothing else breaks.

## Acceptance Criteria

- [ ] `src/library/data/Notary.Data.csproj` references `AutoMapper` 16.3.0
- [ ] No other `.csproj` or `Directory.Packages.props` references AutoMapper at another version
- [ ] `dotnet build` succeeds with no new warnings
- [ ] `dotnet test` passes, including the repository and mapping tests (for example `CaPurposePersistenceTest`)
- [ ] The maintainer is told what to exercise in UAT: create a CA, issue a certificate, open the CA detail page and download a certificate, since all of them go through the mapper

## Technical Notes

Only one reference exists today (`Notary.Data.csproj:10`). Check the 16.1.1 to 16.3.0 release notes for behavior changes that touch `AfterMap`, `ReverseMap`, or null-collection handling, which `ModelMapProfile` relies on. Do not edit `CHANGELOG.md`.

## Dependencies

None.
