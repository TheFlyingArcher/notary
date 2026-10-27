---
run: run-specsmd-fire-planner-02371b-007
work_item: child-ca-key-settings-ui
intent: mixed-chain-key-strength
mode: confirm
checkpoint: plan
approved_at: 2026-10-26T00:00:00Z
---

# Implementation Plan: Child CA key settings UI

## Approach

The key options the page offers (RSA 2048/3072/4096 and P-256/P-384/P-521) are filtered by the parent's strength, and the key choice becomes editable when a parent is selected (today the key radios are disabled for a child because it used to inherit).

**Testable logic in `core`** (the test project does not reference the web project, so the filtering lives where it can be unit tested, and the leaf UI item reuses it):
- New `KeyOptions` static class in `Notary.Security` with the offered `RsaLengths` and `Curves`, and:
  - `AllowedRsaLengths(KeySpec issuer)` and `AllowedCurves(KeySpec issuer)`: the options at least as strong as the issuer, using `KeyStrength.MeetsFloor`; a null issuer (a root) returns every option.
  - `Describe(KeySpec issuer)`: text such as "at least 128-bit (RSA 3072 or P-256 equivalent)" for the page note.

**Page (`CreateCertificateAuthority.razor` and `.razor.cs`):**
- Remove `Disabled="ViewModel.SelectedCa!=null"` from the algorithm, RSA length and curve radio groups.
- Algorithm: show RSA only if any RSA length qualifies and Elliptic Curve only if any curve qualifies. Under a P-384 parent, for example, RSA is not offered at all because RSA-4096 is 128 bits.
- Lengths and curves: show only the allowed ones.
- Defaults: selecting a parent already pre-fills the parent's algorithm, curve and length (an equal-strength choice); that stays.
- When the user switches algorithm, snap length or curve to the weakest allowed option of the new type so the form never holds a value below the floor (`OnKeyTypeChanged`).
- A short info note inside the key section: "This key must be at least as strong as the parent CA's." plus the `Describe` text. Roots show no note and keep every option.
- A server rejection already shows through the existing snackbar.

## Files to Create

| File | Purpose |
|------|---------|
| `src/library/core/Security/KeyOptions.cs` | Offered key options filtered by an issuer's strength, shared with the leaf UI |
| `src/test/KeyOptionsTest.cs` | Unit tests for the filtering |

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/Pages/CreateCertificateAuthority.razor` | Enable the key radios for a child, offer only allowed algorithms, lengths and curves, add the floor note |
| `src/web/Pages/CreateCertificateAuthority.razor.cs` | Parent key spec, allowed options, `OnKeyTypeChanged` snapping |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/KeyOptionsTest.cs` | Root offers everything; P-256 parent offers RSA 3072/4096 and every curve; P-384 parent offers no RSA and P-384/P-521; P-521 parent offers P-521 only; RSA-2048 parent offers all; RSA-3072 parent equal to P-256; an unreadable parent offers nothing |

## Technical Details

- The razor page itself has no automated test project; it is verified by `dotnet build` and by UAT in Visual Studio (create a root, then an intermediate under it, and check the key section for an RSA, P-256, P-384 and P-521 parent).
- The server-side rule from the previous items is still the authority; the page only avoids offering what would be rejected.
- Roots, the self-signed pages and the leaf page are not changed here.

---
*Plan approved at checkpoint. Execution follows.*
