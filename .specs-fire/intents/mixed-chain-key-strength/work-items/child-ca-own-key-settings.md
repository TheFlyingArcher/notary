---
id: child-ca-own-key-settings
title: Child CA own key settings
intent: mixed-chain-key-strength
complexity: medium
mode: confirm
status: completed
depends_on:
  - key-strength-model
created: 2026-10-26T00:00:00Z
run_id: run-specsmd-fire-planner-02371b-005
completed_at: 2026-10-27T00:19:39.768Z
---

# Work Item: Child CA own key settings

## Description

Stop `CertificateAuthorityService.CreateAsync` copying the parent's key algorithm, curve and length into a child, and validate the child's own settings against its parent.

## Acceptance Criteria

- [ ] A child CA keeps the algorithm, curve and length it was created with
- [ ] An RSA child without a length, or an EC child without a curve, is rejected clearly
- [ ] A child weaker than its direct parent is rejected with `Result.Fail`, next to the existing `CaProfileCatalog.CheckIssuer` check
- [ ] A child at equal or higher strength is accepted, including across RSA and EC
- [ ] Root CAs and existing chains are unaffected
- [ ] `dotnet test ./src/test/` passes

## Technical Notes

The parent's key settings are on the stored `CertificateAuthority`. Compare against the direct parent only; a legacy violation higher up must not block new issuance. Signature algorithm already follows the issuer key (#46).

## Dependencies

- key-strength-model
