---
id: upgrade-automapper-security
title: Upgrade AutoMapper to fix Dependabot alert
status: completed
created: 2026-11-14T00:00:00Z
completed_at: 2026-11-14T16:40:12.768Z
---

# Intent: Upgrade AutoMapper to fix Dependabot alert #3

## Goal

Upgrade the `AutoMapper` NuGet package from 16.1.1 to 16.3.0 so Notary no longer ships a version flagged by Dependabot alert #3 (GHSA-6vhm-j9h7-f9gh, CVE-2026-128082, severity high).

## Users

The maintainer, who needs the security alert closed, and every Notary operator running the API and web UI.

## Problem

AutoMapper `>= 16.0.0, < 16.3.0` does not enforce `MaxDepth` when recursion goes through constructor parameters. A deeply nested source mapped to a constructor-built destination (such as a positional record) overflows the stack and kills the process.

Notary's exposure looks low: `ModelMapProfile` only has flat DTO-to-model maps, with no self-referential types. The alert stays open until the package is upgraded, and there is no reason to carry a known-vulnerable version.

## Success Criteria

- `src/library/data/Notary.Data.csproj` references AutoMapper 16.3.0
- The solution builds cleanly and all existing tests pass, with no mapping behavior changes (the null-usages `AfterMap` handling in `ModelMapProfile` still works)
- No other project pins an older AutoMapper
- Dependabot alert #3 closes once the change merges to `main`

## Constraints

- Bump only: no mapper rewrite, no new tests beyond what the existing suite covers
- Do not edit `CHANGELOG.md`; the release workflow generates it
- Stays runnable and debuggable from Visual Studio (UAT workflow)
- Out of scope: removing AutoMapper, adding recursion guard tests

## Notes

Source: https://github.com/TheFlyingArcher/notary/security/dependabot/3

Fixed versions per the advisory: 16.3.0 and 15.1.4. We are on the 16.x line, so 16.3.0 is the target.

Scope was confirmed with the maintainer as "bump only".
