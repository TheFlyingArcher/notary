---
id: key-strength-ux-clarity
title: Explain equal security levels in the key pickers
intent: mixed-chain-key-strength
complexity: medium
mode: confirm
status: completed
depends_on:
  - child-ca-key-settings-ui
created: 2026-10-26T00:00:00Z
run_id: run-specsmd-fire-planner-02371b-008
completed_at: 2026-10-27T00:53:17.623Z
---

# Work Item: Explain equal security levels in the key pickers

## Description

UAT found that an RSA-4096 parent offers RSA-3072 for a child, which looks like a downgrade although both are 128-bit security under NIST SP 800-57. The rule stays as agreed (compare security bits only). Change the key picker so equal strength reads as the same level, not a smaller key.

## Acceptance Criteria

- [ ] Each offered key option shows its security level, for example "RSA 3072 bit - 128-bit security" and "P-384 - 192-bit security"
- [ ] Under a parent, each option carries a neutral label: "Same as parent", "Same security level" or "Stronger"; no warning colors or icons
- [ ] The parent's own key and security level are shown above the radios
- [ ] The floor note explains that keys are compared by security level, not raw size, and gives the RSA 3072 / RSA 4096 example when it applies
- [ ] The labels and wording come from `core` so the leaf page can reuse them, with unit tests (RSA-4096 parent: 3072 is "Same security level", 4096 is "Same as parent"; P-256 parent: RSA-3072 is "Same security level"; P-384 parent: P-521 is "Stronger")
- [ ] No change to the rule or to which options are offered
- [ ] `dotnet build` and `dotnet test ./src/test/` pass

## Technical Notes

Add label/relationship helpers to `KeyOptions` in `src/library/core/Security`. Update the key section of `CreateCertificateAuthority.razor` and `.razor.cs`. Replace the `KeyOptions.Describe` text. UAT in Visual Studio.

## Dependencies

- child-ca-key-settings-ui
