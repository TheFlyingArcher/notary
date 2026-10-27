---
id: child-ca-key-settings-ui
title: Child CA key settings UI
intent: mixed-chain-key-strength
complexity: medium
mode: confirm
status: completed
depends_on:
  - key-strength-model
  - child-ca-own-key-settings
created: 2026-10-26T00:00:00Z
run_id: run-specsmd-fire-planner-02371b-007
completed_at: 2026-10-27T00:31:35.883Z
---

# Work Item: Child CA key settings UI

## Description

On the Create Certificate Authority page, default a child's key algorithm, curve and length to its parent's and offer only options that meet the strength floor.

## Acceptance Criteria

- [ ] Choosing a parent pre-fills algorithm, curve and length from the parent
- [ ] Algorithm, curve and length options below the parent's strength are not offered
- [ ] A user can still raise strength or switch between RSA and EC when the strength is at least the parent's
- [ ] Root CAs keep all options
- [ ] A server-side rejection is shown as an error, not swallowed
- [ ] `dotnet build` passes

## Technical Notes

Pages: `CreateCertificateAuthority.razor` and `.razor.cs`, with `CreateCertificateAuthorityViewModel`. Reuse the shared strength model for filtering. UAT in Visual Studio.

## Dependencies

- key-strength-model
- child-ca-own-key-settings
