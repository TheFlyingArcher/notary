---
run: run-specsmd-fire-planner-02371b-012
work_item: changelog-homepage
intent: homepage-changelog
mode: confirm
checkpoint: plan
approved_at: pending
---

# Implementation Plan: Homepage shows the change log

## Approach

Parsing lives in the core library so it can be unit tested (the test project does not reference the web project). The web project reads the file once, renders the notes, and the homepage lists the entries.

1. **Parser** (`Notary.Changelog`): `ChangelogParser.Parse(string)` returns a list of `ChangelogEntry(Version, Date, Notes)` in file order. An entry starts at a line matching `## v<n>.<n>.<n> - <yyyy-MM-dd>`. The header comment, the `# Changelog` title and anything before the first entry are ignored. A heading with a bad date or malformed version is not an entry, so it stays part of the previous entry's notes. Blank input returns an empty list.
2. **Provider** (web `Services/ChangelogProvider`): reads `CHANGELOG.md` from `AppContext.BaseDirectory` once and caches the parsed entries. Missing or unreadable file returns a failed `Result` with a message rather than throwing; no log of file contents.
3. **Markdown**: add the `Markdig` package to the web project. Notes are converted to HTML with raw HTML disabled, then shown with `MarkupString`. A small helper keeps the pipeline in one place.
4. **Homepage**: `Index.razor` shows a "Change log" title and one `MudPaper` per release (version, date, rendered notes), newest first, in the normal page scroll. No entries or a read failure shows a friendly `MudAlert`. All statistics, lorem ipsum, "Please wait" and `ICertificateService` use are removed. `[Authorize]` stays.
5. Register the provider as a singleton in `Program.cs`.

## Files to Create

| File | Purpose |
|------|---------|
| `src/library/core/Changelog/ChangelogEntry.cs` | Positional record: version, date, notes |
| `src/library/core/Changelog/ChangelogParser.cs` | Parses `CHANGELOG.md` text into entries |
| `src/web/Services/ChangelogProvider.cs` | Reads and caches the file; returns `Result` |
| `src/web/Services/ChangelogMarkdown.cs` | Markdown to safe HTML (raw HTML disabled) |
| `src/test/ChangelogParserTest.cs` | Parser tests |

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/Pages/Index.razor` | Replace tiles with the change log list and empty/error alert |
| `src/web/Pages/Index.razor.cs` | Remove certificate loading; inject the provider |
| `src/web/Notary.Web.csproj` | Add `Markdig` package reference |
| `src/web/Program.cs` | Register `ChangelogProvider` as a singleton |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/ChangelogParserTest.cs` | Multiple entries keep file order; version and date parsed; header and title ignored; notes keep their level-4 headings and blank lines; empty and whitespace input; no entries; malformed heading stays in notes; the real `src/web/CHANGELOG.md` parses to 6 entries in descending date order |

## Technical Details

- Allman braces, file-scoped namespaces, positional record, `Result<T>` for the read failure, no exceptions for expected cases.
- Raw HTML in notes is disabled. Markdown links are not filtered; the file is written by repo owners from their own releases, so this is accepted.
- The page can only be viewed in Visual Studio for UAT. I will also run the app's publish to check that the file and Markdig are in the output.

---
*Plan awaiting approval.*
