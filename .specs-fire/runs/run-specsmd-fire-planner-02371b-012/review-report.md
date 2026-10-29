---
run: run-specsmd-fire-planner-02371b-012
work_item: changelog-homepage
intent: homepage-changelog
generated: 2026-10-30T00:25:00Z
---

# Code Review: Homepage shows the change log

Files reviewed: `src/library/core/Changelog/*.cs`, `src/web/Services/ChangelogProvider.cs`, `src/web/Services/ChangelogMarkdown.cs`, `src/web/Pages/Index.razor`, `src/web/Pages/Index.razor.cs`, `src/web/Program.cs`, `src/web/wwwroot/css/site.css`, `src/test/ChangelogParserTest.cs`

## Auto-fixed

- `Index.razor.cs`: made `LoadError` nullable, so the new code adds no nullability warning.

## Checked, no change needed

- Expected failures (missing or unreadable file) return `Result`, not exceptions. Only `IOException` and `UnauthorizedAccessException` are caught, and file contents are never logged.
- Raw HTML in the notes is disabled, and the output was checked to escape `<script>`.
- The provider reads the file once and caches it, so the page does no file I/O per visit.
- `Program.cs` and `site.css` keep their CRLF line endings; new files follow the LF files around them.
- `[Authorize]` stays on the page.

## Suggestions (not applied)

- Markdown links are not filtered, so a `javascript:` link in a release note would render as a link. The notes come from the repo owners' own releases, so this was accepted in the plan.
- A change to `CHANGELOG.md` is only picked up after a restart, because the file is cached. That matches how the file is delivered (inside the image).
