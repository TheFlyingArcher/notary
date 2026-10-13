---
run: run-specsmd-fire-planner-01019e-005
work_item: certificates-page-create-buttons
intent: organize-certificates-by-ca
generated: 2026-10-14T01:00:00Z
status: passed-pending-uat
---

# Test Report: Create buttons on the Certificates page

## Summary

310 passed, 0 failed (regression run; markup-only change). `Notary.Web` builds cleanly.

## Acceptance Criteria Validation

- ✅ **Create Certificate links to /certificates/create?ca={selected slug}** — `CreateCertificateHref`, slug URL-escaped
- ✅ **Create Self-Signed Certificate links to the self-signed form** — implemented
- ✅ **Both disabled without NotaryAdmin/NotaryWriter** — inside the existing AuthorizeView
- ⚠️ **End-to-end flow verified in the running UI** — pending your UAT (no MongoDB or Auth0 here)
