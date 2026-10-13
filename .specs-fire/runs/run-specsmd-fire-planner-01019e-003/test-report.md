---
run: run-specsmd-fire-planner-01019e-003
work_item: create-certificate-scoped-to-ca
intent: organize-certificates-by-ca
generated: 2026-10-14T00:15:00Z
status: passed-pending-uat
---

# Test Report: Create Certificate scoped to the selected CA

## Summary

309 passed, 0 failed (regression run; no new automated tests, page-only change). `Notary.Web` and `Notary.Setup` build cleanly.

## Acceptance Criteria Validation

- ✅ **/certificates/create?ca={slug} issues from that CA with existing profile, subject and validity behavior** — the existing handler logic now runs once in `ApplyCa`; submit unchanged
- ✅ **Select CA dropdown gone; CA name shown read-only** — implemented
- ✅ **Missing, unknown, revoked or non-leaf-issuing ca redirects with a message** — eligibility comes from `GetLeafIssuingAuthoritiesAsync` (tested); redirect plus snackbar implemented
- ⚠️ **Verified in the running UI** — NOT done by me (no MongoDB or Auth0 here). Needs your UAT.

## Test Commands

```bash
dotnet test ./src/test/
```
