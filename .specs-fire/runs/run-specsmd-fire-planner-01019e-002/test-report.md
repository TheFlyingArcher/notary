---
run: run-specsmd-fire-planner-01019e-002
work_item: certificates-ca-dropdown-ui
intent: organize-certificates-by-ca
generated: 2026-10-13T23:50:00Z
status: passed-pending-uat
---

# Test Report: CA dropdown on the Certificates page

## Summary

309 passed, 0 failed, 0 skipped (regression run; no new automated tests, since this item is page-only code and the queries were tested in the previous run). `Notary.Web` builds cleanly.

## Acceptance Criteria Validation

- ✅ **Dropdown lists leaf-issuing, non-revoked CAs plus Self-signed** — implemented from `GetLeafIssuingAuthoritiesAsync`
- ✅ **Selecting an entry filters the table to that scope** — implemented (per-CA via the CA certificate slug; self-signed via the self-signed query)
- ✅ **Last selection persists, falls back to the first CA** — implemented with `ProtectedLocalStorage`; falls back to Self-signed with no CAs
- ✅ **Empty scope and no-CA messages** — implemented (`NoRecordsContent`, info alert)
- ⚠️ **Verified in the running UI** — NOT done by me. The app needs MongoDB and Auth0 sign-in, neither available here. Needs UAT in Visual Studio.

## Test Commands

```bash
dotnet test ./src/test/
```
