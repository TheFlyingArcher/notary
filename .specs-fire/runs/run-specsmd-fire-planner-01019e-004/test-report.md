---
run: run-specsmd-fire-planner-01019e-004
work_item: self-signed-certificate-form
intent: organize-certificates-by-ca
generated: 2026-10-14T00:45:00Z
status: passed-pending-uat
---

# Test Report: Self-signed certificate form

## Summary

310 passed, 0 failed (1 new). `Notary.Web` builds cleanly.

## Acceptance Criteria Validation

- ✅ **Issues a self-signed certificate with the advanced options** — new test `SelfSignedLeaf_HasNoIssuer_IsNotACa_AndCarriesTheChosenUsages` covers the service; the form builds the request with no parent
- ✅ **Advanced usage pickers and key algorithm settings editable** — implemented in the new page
- ✅ **Authorization matches Create Certificate** — same `NotaryAdmin,NotaryWriter` attribute
- ⚠️ **Verified in the running UI** — not done by me; deferred to the end-to-end UAT as agreed

## Test Commands

```bash
dotnet test ./src/test/
```
