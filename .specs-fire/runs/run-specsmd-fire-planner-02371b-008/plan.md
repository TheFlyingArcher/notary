---
run: run-specsmd-fire-planner-02371b-008
work_item: key-strength-ux-clarity
intent: mixed-chain-key-strength
mode: confirm
checkpoint: plan
approved_at: 2026-10-26T00:00:00Z
---

# Implementation Plan: Explain equal security levels in the key pickers

## Approach

Keep the rule and the offered options exactly as they are; change only how the options are described.

**In `core`** (`KeyOptions`, so the leaf page reuses it and it is unit tested):
- New `enum KeyRelation { SameAsParent, SameSecurityLevel, Stronger }` and `KeyRelation? Relate(KeySpec issuer, KeySpec candidate)`: `SameAsParent` when algorithm and curve/length match the issuer's; otherwise `SameSecurityLevel` when the security bits are equal, `Stronger` when higher; null when weaker or unreadable (such options are never offered).
- `string Label(KeySpec candidate)`: "RSA 3072 bit - 128-bit security", "P-384 - 192-bit security". An unreadable key gets its plain name.
- `string RelationText(KeyRelation)`: "Same as parent", "Same security level", "Stronger".
- `Describe(KeySpec issuer)` is rewritten to: "Keys are compared by security level, not raw size. {issuer label} gives {n}-bit security, so any option below at that level or higher is accepted." plus, when the issuer is RSA 4096 or 3072, "For example, RSA 3072 and RSA 4096 both give 128-bit security." The unreadable-parent text stays.
- `string DescribeIssuer(KeySpec issuer)`: "Parent CA key: RSA 4096 bit - 128-bit security".

**Page** (`CreateCertificateAuthority.razor` and `.razor.cs`), key section only:
- Show the parent's key line above the radios.
- Each radio shows its `Label` and a neutral `MudChip` with `RelationText` (default color, outlined, no icon). Algorithm radios stay plain.
- The existing info note uses the rewritten `Describe`.
- Roots (no parent) show plain labels with no chips and no note, as now.

No change to `MeetsFloor`, `CheckNotWeaker`, the server, or which options appear.

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/core/Security/KeyOptions.cs` | `KeyRelation`, `Relate`, `Label`, `RelationText`, `DescribeIssuer`; rewritten `Describe` |
| `src/test/KeyOptionsTest.cs` | Tests for the relation, labels and wording; the existing `Describe` tests updated |
| `src/web/Pages/CreateCertificateAuthority.razor` | Parent key line, labelled radios with neutral chips |
| `src/web/Pages/CreateCertificateAuthority.razor.cs` | Helpers that call `KeyOptions` for the labels |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/KeyOptionsTest.cs` | RSA-4096 parent: 3072 is SameSecurityLevel, 4096 is SameAsParent; P-256 parent: RSA-3072 SameSecurityLevel, P-256 SameAsParent, P-384 Stronger; P-384 parent: P-521 Stronger; weaker or unreadable returns null; labels and note text including the RSA 3072/4096 example |

## Technical Details

- The razor page has no automated test project; it is verified by `dotnet build` and your UAT in Visual Studio.
- Wording avoids "smaller", "weaker" or "downgrade" for allowed options.

---
*Plan approved at checkpoint. Execution follows.*
