---
run: run-specsmd-fire-planner-01019e-002
work_item: certificates-ca-dropdown-ui
intent: organize-certificates-by-ca
generated: 2026-10-13T23:55:00Z
mode: confirm
---

# Implementation Walkthrough: CA dropdown on the Certificates page

## Summary

`/certificates` now has an "Issuing CA" dropdown ("Self-signed" plus each non-revoked, leaf-issuing CA by name). The table lists only the selected scope. The last choice is stored in the browser (`ProtectedLocalStorage`, key `certificates.selectedCa`); the default is the first CA, or Self-signed when no CAs exist.

## Files Changed

| File | Changes |
|------|---------|
| `src/web/Pages/Certificates.razor` | Dropdown, no-CA alert, per-scope empty message |
| `src/web/Pages/Certificates.razor.cs` | Selection state, loading by scope, storage read/write; loading happens in `OnAfterRenderAsync` because JS interop is needed |

## How to Verify (UAT)

1. Open Certificates with at least two CAs: the first CA by name is selected and only its certificates appear.
2. Switch to another CA and to Self-signed; the table changes.
3. Reload: the last choice is kept.
4. Pick a CA with no certificates: a message replaces the empty table.
5. With no leaf-issuing CAs, the info alert shows and Self-signed is selected.

The Create Certificate button is unchanged until the last work item.
