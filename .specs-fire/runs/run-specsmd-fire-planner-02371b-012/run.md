---
id: run-specsmd-fire-planner-02371b-012
scope: single
work_items:
  - id: changelog-homepage
    intent: homepage-changelog
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
current_item: null
status: completed
started: 2026-10-29T23:01:01.609Z
completed: 2026-10-29T23:05:24.772Z
---

# Run: run-specsmd-fire-planner-02371b-012

## Scope
single (1 work item)

## Work Items
1. **changelog-homepage** (confirm) — completed


## Current Item
(all completed)

## Files Created
- `src/library/core/Changelog/ChangelogEntry.cs`: Record for one release
- `src/library/core/Changelog/ChangelogParser.cs`: Parses CHANGELOG.md into entries
- `src/web/Services/ChangelogProvider.cs`: Reads and caches the file, returns Result
- `src/web/Services/ChangelogMarkdown.cs`: Markdown to HTML with raw HTML disabled
- `src/test/ChangelogParserTest.cs`: Parser tests

## Files Modified
- `src/web/Pages/Index.razor`: Replaced the tiles with the change log list and an empty/error alert
- `src/web/Pages/Index.razor.cs`: Removed certificate loading; reads entries from ChangelogProvider
- `src/web/Notary.Web.csproj`: Added Markdig
- `src/web/Program.cs`: Registered ChangelogProvider as a singleton
- `src/web/wwwroot/css/site.css`: Spacing for rendered release notes

## Decisions
(none)


## Summary

- Work items completed: 1
- Files created: 5
- Files modified: 5
- Tests added: 17
- Coverage: 0%
- Completed: 2026-10-29T23:05:24.772Z
