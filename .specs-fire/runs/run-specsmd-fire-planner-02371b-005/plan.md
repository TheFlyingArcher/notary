---
run: run-specsmd-fire-planner-02371b-005
work_item: child-ca-own-key-settings
intent: mixed-chain-key-strength
mode: confirm
checkpoint: plan
approved_at: 2026-10-26T00:00:00Z
---

# Implementation Plan: Child CA own key settings

## Approach

In `CertificateAuthorityService.CreateAsync`, replace the block that copies `KeyCurve`, `KeyAlgorithm` and `KeyLength` from the parent with a strength check against the parent, using `KeyStrength.CheckNotWeaker` from the previous item.

- Keep the existing `CaProfileCatalog.CheckIssuer` call, then build `KeySpec`s from the parent and from the child's own settings and call `CheckNotWeaker(parentSpec, childSpec)`. A failure returns `Result<CertificateAuthority>.Fail(status, error)`; nothing is issued or saved.
- The child's `KeyAlgorithm`, `KeyCurve` and `KeyLength` are no longer overwritten. `ParentCaSlug = parentCa.Slug` stays.
- The existing guard that an RSA CA must supply a key length stays as it is for roots and children.
- An EC child without a curve, or a child whose settings cannot be read, fails as `Invalid` through the model.
- If the parent's own strength cannot be read (an RSA parent with no stored length, an EC parent with no curve), the check fails with "The issuing CA key: ..." rather than guessing. Parents were always created with these values, so this is a safety net, not an expected path.
- Compared against the direct parent only; an old violation higher in the chain does not matter. Roots (no parent) are unaffected.

The web page already submits the user's algorithm, curve and length (pre-filled from the parent), so existing UI flows produce an equal-strength child and still pass. The UI filtering is a later item.

Two existing tests build a child under `MockCa()` whose key fields are placeholders (RSA with length 0), which the new check would rightly reject; I will give those tests explicit, valid parent and child key settings.

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/service/CertificateAuthorityService.cs` | Replace the key-settings copy with the parent strength check |
| `src/test/CertificateAuthorityServiceTest.cs` | Give the two existing child tests valid key settings; add tests for the new behavior |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/CertificateAuthorityServiceTest.cs` | Child keeps its own algorithm/curve/length and the issuance request carries them; RSA root with EC child and EC root with RSA child accepted at equal or higher strength; child weaker than parent rejected with nothing issued or saved; EC child without a curve rejected; root unaffected |

## Technical Details

- Uses the existing mocks (`_caRepo`, `_certificateService`) and asserts on the `CertificateRequest` captured from `IssueCertificateAsync` to prove the child's own settings reach issuance.
- Signing already follows the issuer key (#46), so an EC child under an RSA parent signs correctly.
- The leaf and CA-certificate issuance path gets its own floor check in the next item; this item covers CA creation.

---
*Plan approved at checkpoint. Execution follows.*
