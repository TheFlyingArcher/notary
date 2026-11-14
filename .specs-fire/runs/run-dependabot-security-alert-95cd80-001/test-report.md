# Test Report: Bump AutoMapper to 16.3.0

Run: run-dependabot-security-alert-95cd80-001

## Test Results
- Passed: 739
- Failed: 0
- Skipped: 0

Command: `dotnet build src/Notary.sln` (0 errors, 169 pre-existing nullable warnings in `src/web`, none from AutoMapper) then `dotnet test src/Notary.sln --no-build`.

## Coverage
No new code; coverage unchanged. No tests added (bump only, per intent scope).

## Acceptance Criteria Validation
- [x] `Notary.Data.csproj` references AutoMapper 16.3.0
- [x] No other `.csproj`/props file references AutoMapper
- [x] Build succeeds with no new warnings
- [x] All 739 tests pass, including the mapping/repository tests
- [x] `dotnet list package --vulnerable` no longer reports AutoMapper

## Notes
`dotnet list package --vulnerable` still reports `Aspire.Hosting.AppHost` / `Aspire.Hosting` 13.4.6 (GHSA-f7vh-h5g3-7jrh, High) and transitive `MessagePack` 2.5.302 (GHSA-qhrr-8q5h-9q3h, Moderate). These are outside this work item.
