---
id: changelog-homepage
title: Homepage shows the change log
intent: homepage-changelog
complexity: medium
mode: confirm
status: completed
depends_on: [changelog-file-and-backfill]
created: 2026-10-29T00:00:00Z
---

# Work Item: Homepage shows the change log

## Description

Replace the homepage tiles with the change log read from `CHANGELOG.md`: releases newest first in a scrollable layout, notes rendered as Markdown. Remove the statistics tiles, the lorem ipsum tile, the "Please wait" gate and the certificate loading.

## Acceptance Criteria

- [ ] `/` lists releases newest first, scrolling back to v1.0.15
- [ ] Release notes render as Markdown
- [ ] The tiles, lorem ipsum, "Please wait" gate and `ICertificateService` use are gone from `Index`
- [ ] A missing or empty `CHANGELOG.md` shows a friendly message instead of an error
- [ ] Parser tests cover multiple entries, ordering, and a malformed or empty file
- [ ] Solution builds cleanly and existing tests pass; page checked in Visual Studio for UAT

## Technical Notes

`src/web/Pages/Index.razor` and `Index.razor.cs` (still `[Authorize]`). Put the parser in a testable class and read the file once, cached. Check whether `Notary.Web.csproj` already references Markdig before adding it. Allman braces, NUnit 4 + Moq.

## Dependencies

- changelog-file-and-backfill
