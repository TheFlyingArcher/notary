---
run: run-specsmd-fire-planner-6274df-003
work_item: wrap-service-calls
intent: loading-overlay
generated: 2026-11-08T05:35:00Z
---

# Code Review: Wrap page and dialog service calls

## Auto-fixed

None needed.

## Findings

No blocking issues. Notes:

- **Overlay vs inline spinners** — pages keep their own `IsLoading` / `_isLoading` UI, so a skeleton or table spinner shows behind the overlay. Harmless; removing them is a separate cleanup.
- **`CreateCertificateAuthority` handler** — the change handler wraps `GetAsync` inside an existing `Task.Run`; the overlay covers it, but the field copying after it runs outside any tracked operation. It is synchronous, so it adds no wait.
- **`OnParentCaListChange`** — the `GetAsync` is wrapped, but the handler is awaited by MudSelect, so a second selection during the overlay delay (under 200 ms) could race as it could before. Not made worse.
- **Dialogs** — `_busy` is reset in `finally`, so a failed call leaves the dialog usable. Cancel is disabled while busy.
- **`ShowMessageBoxAsync`, `ShowAsync`, `dialog.Result`** — intentionally unwrapped.
- **Not covered by the overlay** — `CrlController` and `AuthenticateController` are HTTP endpoints, not UI.

## Suggestions needing confirmation

None.
