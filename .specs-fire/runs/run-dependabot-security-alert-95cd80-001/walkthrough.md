# Walkthrough: Bump AutoMapper to 16.3.0

Run: run-dependabot-security-alert-95cd80-001 | Intent: upgrade-automapper-security | Mode: autopilot

## Summary

Upgraded AutoMapper from 16.1.1 to 16.3.0 to close Dependabot alert #3 (GHSA-6vhm-j9h7-f9gh, CVE-2026-128082, high): `MaxDepth` was not enforced when recursion went through constructor parameters, so deeply nested input could overflow the stack and crash the process.

## Changes

- `src/library/data/Notary.Data.csproj`: `AutoMapper` 16.1.1 to 16.3.0. It was the only AutoMapper reference in the repo.

## Decisions

- Bump only, as agreed during planning. `ModelMapProfile` has only flat DTO-to-model maps, so Notary was never exposed to the recursion path; no code or test changes were needed.
- Left `CHANGELOG.md` alone because the release workflow generates it.

## Deviations from plan

None.

## Dependencies

No new packages.

## Verification

- `dotnet build src/Notary.sln`: 0 errors (169 existing nullable warnings in `src/web`, none from this change)
- `dotnet test src/Notary.sln`: 739 passed, 0 failed
- `dotnet list package --vulnerable`: AutoMapper no longer reported

## UAT in Visual Studio

Every path below goes through the mapper:
1. Create a root CA, then an intermediate CA under it (check custom key usages stay unset on a default CA)
2. Issue a leaf certificate and a self-signed certificate
3. Open the CA detail page (info, CRL, issuer tree, issued certificates table)
4. Download a certificate in a couple of formats and revoke one

## Developer notes

- `dotnet list package --vulnerable` still reports `Aspire.Hosting.AppHost`/`Aspire.Hosting` 13.4.6 (GHSA-f7vh-h5g3-7jrh, High) and transitive `MessagePack` 2.5.302 (GHSA-qhrr-8q5h-9q3h, Moderate). Not part of this intent.
- The FIRE scripts need the `yaml` npm package. I installed it in a scratch folder and used `NODE_PATH` so no `node_modules` landed in the repo.
