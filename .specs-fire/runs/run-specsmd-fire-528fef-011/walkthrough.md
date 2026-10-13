---
run: run-specsmd-fire-528fef-011
work_item: custom-ca-usages-ui
intent: ca-purpose-profiles
generated: 2026-10-13T10:10:00Z
mode: confirm
---

# Implementation Walkthrough: Custom CA usage pickers

## Summary

Choosing Custom on the Create CA page now reveals pickers for the key usages and extended key usages the CA will carry, under the existing discouraged warning. The choices are saved with the CA and used to build its certificate. Under a parent that restricts usages, options outside the parent's set are disabled and at least one extended key usage is required. This completes the CA purposes feature.

## Structure Overview

The service already turned a Custom CA's chosen usages into its certificate. This item adds the page controls and fixes one policy gap: Custom was disabled under a restricted parent only because there was no way to choose usages. The page sends the two fixed key usages (certificate signing and CRL signing) together with the chosen extras, which marks the CA as a deliberate Custom choice.

## Files Changed

### Created

| File | Purpose |
|------|---------|
| (none) | |

### Modified

| File | Changes |
|------|---------|
| `src/library/core/Profiles/KeyPurposeOids.cs` | `Known` list of extended key usages with names; `GetName` uses it |
| `src/library/core/Profiles/CaProfileCatalog.cs` | Custom offered under a restricted parent; `DescribeKeyUsage` public and named like the certificate form |
| `src/web/ViewModels/CreateCertificateAuthorityViewModel.cs` | Selected custom key usages and extended key usages |
| `src/web/Pages/CreateCertificateAuthority.razor` | Pickers and the "Always included" line, shown for Custom |
| `src/web/Pages/CreateCertificateAuthority.razor.cs` | Option rules under a parent, clearing, validation, save, updated Custom note |
| `src/test/CaProfileCatalogTest.cs` | Updated and new tests |
| `src/test/CaPurposeIssuanceTest.cs` | Real-certificate tests for Custom |

## Key Implementation Details

### 1. What a Custom CA carries

| Choice | Certificate |
|---|---|
| Nothing extra | Certificate Key Sign and CRL Sign only; no extended key usage |
| Extra key usages | Those plus the two fixed ones |
| Extended key usages | Exactly the chosen ones |
| Under a restricted parent | Extended key usages required, and only the parent's are allowed |

### 2. Why the two fixed usages are always sent

A CA with no purpose gets the old broad usages for backward compatibility. Sending the two fixed usages from the page is what tells the service "this was chosen", so a Custom CA with nothing extra is a minimal CA, not a broad one.

### 3. Under a restricted parent

Options outside the parent's extended key usages are disabled with "not allowed by the {parent} CA". Submitting with none chosen shows a message. Changing the parent to one that does not allow a choice clears it. The server enforces the same rules.

## Security Considerations

| Concern | Approach |
|---------|----------|
| A Custom CA widening its parent's usages | Disabled in the page and rejected by the server before any key exists |
| A deliberate Custom CA silently getting broad usages | The fixed usages are always sent, and a test proves the result |
| Users making a risky choice | Marked discouraged with a warning, and the purposes above are preferred |

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Custom under a restricted parent | Offered; usages required | The user can act on the requirement instead of being blocked |
| "At least one key usage" | Met by the two fixed usages | They are always present |
| Names | Match the certificate form ("Certificate Key Sign", "CRL Sign") | Consistency |

## Deviations from Plan

None. The in-browser check is for UAT.

## Dependencies Added

| Package | Why Needed |
|---------|------------|
| (none) | |

## How to Verify

1. **Tests**
   ```bash
   dotnet test ./src/test/
   ```
   Expected: 302 passed.

2. **UAT in Visual Studio** (restart the app first):
   - Create CA, no parent, choose **Custom**: the warning, the "Always included" line and two pickers appear; they are not shown for other purposes.
   - Create it with nothing extra chosen, then inspect the certificate (`certutil -dump`): Key Usage is Certificate Signing and CRL Signing only, with no Enhanced Key Usage.
   - Create a TLS/SSL CA, then a Custom CA under it: only Server Authentication is selectable; submitting with none chosen shows the message.
   - Switch the parent while Client Authentication is ticked: the choice clears.

## Test Coverage

- Tests added: 12
- Coverage: not measured
- Status: passing (302)

## Ready for Review

- [ ] All acceptance criteria met (in-browser verification outstanding)
- [x] Tests passing
- [x] No critical issues
- [x] Documentation updated (if applicable)
- [x] Developer notes captured

## Developer Notes

- The extended key usage list now exists in two places (here and the certificate form). A follow-up could consolidate them.
- With this item all seven work items of the intent are built.

---
*Generated by specs.md - fabriqa.ai FIRE Flow Run run-specsmd-fire-528fef-011*
