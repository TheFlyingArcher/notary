---
run: run-specsmd-fire-planner-02371b-009
work_item: leaf-key-settings-ui
intent: mixed-chain-key-strength
mode: confirm
checkpoint: plan
approved_at: 2026-10-26T00:00:00Z
---

# Implementation Plan: Leaf key settings UI

## Approach

Give the Create Certificate page the same key picker the Create CA page now has, driven by the issuing CA's key and the shared wording in `KeyOptions`.

**Page (`CreateCertificate.razor` and `.razor.cs`):**
- The issuing CA's key (`ViewModel.SelectedCa`) becomes a `KeySpec`; the allowed RSA sizes and curves come from `KeyOptions.AllowedRsaLengths` / `AllowedCurves`, so nothing below the CA's strength is offered.
- Remove `Disabled="ViewModel.SelectedCa!=null"` from the algorithm, size and curve radios. The key still defaults to the CA's own (as `ApplyCa` already does).
- Algorithm radios: RSA only if a size qualifies, Elliptic Curve only if a curve qualifies. Each option shows its label with the security level and a neutral outlined chip ("Same as ...", "Same security level", "Stronger"). A line above the radios names the issuing CA's key, and the info note explains that keys are compared by security level.
- `OnKeyAlgorithmChanged`: snap the size or curve to the weakest allowed option of the new type, then call the existing `RefreshUsages()` so locked purpose usages and the available key usages follow the new algorithm (an EC key cannot encipher; a purpose CA fixes the leaf usages per algorithm).
- The request already carries the chosen algorithm, size or curve; a server rejection already shows through the snackbar.
- The self-signed page is not changed.

**Wording (`KeyOptions`)**: the leaf's issuer is a CA, not a "parent", so the shared text says "issuing CA" on both pages: "Issuing CA key: RSA 4096 bit - 128-bit security", chips "Same as issuing CA", and "The issuing CA's key gives 128-bit security...". This changes the wording just added to the Create CA page, where the page already labels the parent as "Issuing CA".

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/Pages/CreateCertificate.razor` | Editable, filtered key section with labels, chips and note |
| `src/web/Pages/CreateCertificate.razor.cs` | Issuer key spec, allowed options, `OnKeyAlgorithmChanged` |
| `src/library/core/Security/KeyOptions.cs` | "issuing CA" wording in `DescribeIssuer`, `RelationText` and `Describe` |
| `src/test/KeyOptionsTest.cs` | Update the expected strings |
| `src/web/Pages/CreateCertificateAuthority.razor.cs` | None expected; it reads the text from `KeyOptions` |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/KeyOptionsTest.cs` | Updated wording assertions (relation text, issuer line, note) |

The filtering and relation logic were already unit tested for the CA page, and this page uses the same code, so no new logic needs new tests. The razor page is verified by `dotnet build` and your UAT.

## Technical Details

- The small label/relation helpers are repeated in the code-behind of both pages (about a dozen lines); extracting a shared component would mean reworking the Create CA page, which I am leaving alone.
- The server rule from the previous items stays the authority.

---
*Plan approved at checkpoint. Execution follows.*
