---
run: run-specsmd-fire-planner-02371b-012
work_item: changelog-homepage
intent: homepage-changelog
generated: 2026-10-30T00:30:00Z
mode: confirm
---

# Implementation Walkthrough: Homepage shows the change log

## Summary

The homepage now lists the Notary releases from `CHANGELOG.md`, newest first, in place of the statistics tiles. Parsing is in the core library so it is unit tested; the web project reads the file once, renders the notes as Markdown, and the page shows them.

## Structure Overview

```text
CHANGELOG.md (published beside the app)
  -> ChangelogProvider (web, reads once, returns Result)
  -> ChangelogParser (core, text -> ChangelogEntry list)
  -> Index.razor (one card per release; notes via ChangelogMarkdown -> Markdig)
```

## Files Changed

### Created

| File | Purpose |
|------|---------|
| `src/library/core/Changelog/ChangelogEntry.cs` | Record: version, date, notes |
| `src/library/core/Changelog/ChangelogParser.cs` | Finds `## v1.2.19 - 2026-10-29` headings and collects the notes under each |
| `src/web/Services/ChangelogProvider.cs` | Reads and caches the file; missing or unreadable file gives a failed `Result` |
| `src/web/Services/ChangelogMarkdown.cs` | Markdown to HTML with raw HTML disabled |
| `src/test/ChangelogParserTest.cs` | 17 parser tests, including one against the real file |

### Modified

| File | Changes |
|------|---------|
| `src/web/Pages/Index.razor` | Change log title, a card per release, an alert for no entries or a read failure |
| `src/web/Pages/Index.razor.cs` | Removed certificate loading and the date logic; reads entries from the provider |
| `src/web/Notary.Web.csproj` | Added Markdig 1.5.1 |
| `src/web/Program.cs` | Registered `ChangelogProvider` as a singleton |
| `src/web/wwwroot/css/site.css` | Spacing for the rendered notes |

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Where the parser lives | Core library | The test project does not reference the web project |
| Bad heading | Stays in the previous entry's notes | A typo cannot drop or split a release |
| Raw HTML in notes | Disabled | Notes are shown unescaped via `MarkupString` |
| Link filtering | None | Notes come from the owners' own releases; accepted in the plan |
| Caching | Read once per process | The file ships in the image, so it only changes with a new image |

## How to Verify (UAT in Visual Studio)

1. Run the web project and sign in; `/` should show "Change log" with six releases, v1.2.19 first, v1.0.15 last.
2. Check the notes' headings, bullets, bold text and code look right in both light and dark mode.
3. Temporarily rename `src/web/bin/.../CHANGELOG.md` and restart to see the warning alert instead of an error page.

## Test Coverage

- Tests added: 17 (suite total 651, all passing)
- Publish output contains `CHANGELOG.md` and `Markdig.dll`

## Developer Notes

- The page was not run here, since it needs Auth0 and MongoDB; step 1 above is the check.
- Changing `CHANGELOG.md` takes effect after a restart.
