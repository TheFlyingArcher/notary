---
id: leaf-key-strength-floor
title: Leaf key strength floor
intent: mixed-chain-key-strength
complexity: medium
mode: confirm
status: completed
depends_on:
  - key-strength-model
created: 2026-10-26T00:00:00Z
run_id: run-specsmd-fire-planner-02371b-006
completed_at: 2026-10-27T00:26:21.444Z
---

# Work Item: Leaf key strength floor

## Description

Make `CertificateService.IssueCertificateAsync` reject a leaf whose key is weaker than its issuing CA's key.

## Acceptance Criteria

- [ ] A leaf weaker than its issuing CA is rejected with `Result.Fail` before any key is generated or saved
- [ ] A leaf at equal or higher strength is accepted, including RSA under EC and EC under RSA
- [ ] Self-signed certificates have no issuer and are unaffected
- [ ] The check sits next to the existing `CaProfileCatalog.CheckLeaf` call
- [ ] `dotnet test ./src/test/` passes

## Technical Notes

Issuer key settings come from the issuing CA (or its stored key). Compare against the direct issuer only.

## Dependencies

- key-strength-model
