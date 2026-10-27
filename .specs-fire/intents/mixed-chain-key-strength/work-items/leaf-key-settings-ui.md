---
id: leaf-key-settings-ui
title: Leaf key settings UI
intent: mixed-chain-key-strength
complexity: medium
mode: confirm
status: completed
depends_on:
  - key-strength-model
  - leaf-key-strength-floor
  - key-strength-ux-clarity
created: 2026-10-26T00:00:00Z
run_id: run-specsmd-fire-planner-02371b-009
completed_at: 2026-10-27T01:00:55.055Z
---

# Work Item: Leaf key settings UI

## Description

On the Create Certificate page, offer only key options that meet the issuing CA's strength floor.

## Acceptance Criteria

- [ ] Key algorithm, curve and size options below the issuing CA's strength are not offered
- [ ] The defaults are valid for the selected CA
- [ ] Changing the CA re-filters the options
- [ ] A server-side rejection is shown as an error
- [ ] Self-signed certificates keep all options
- [ ] `dotnet build` passes

## Technical Notes

Pages: `CreateCertificate.razor` and `.razor.cs`, with `CreateCertificateViewModel`. This page already follows the issuing CA's profile; add the strength floor alongside it.

## Dependencies

- key-strength-model
- leaf-key-strength-floor
