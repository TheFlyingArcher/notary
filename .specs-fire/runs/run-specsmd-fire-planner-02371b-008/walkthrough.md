---
run: run-specsmd-fire-planner-02371b-008
work_item: key-strength-ux-clarity
intent: mixed-chain-key-strength
generated: 2026-10-26T00:00:00Z
mode: confirm
---

# Implementation Walkthrough: Explain equal security levels in the key pickers

## Summary

UAT showed that an RSA-4096 parent offers RSA-3072, which looks like a downgrade although both are 128-bit security. The rule is unchanged; the key picker now says so. Each option shows its security level and a neutral label for how it relates to the parent, and a note explains that keys are compared by security level, not raw size.

## Files Changed

### Modified

| File | Changes |
|------|---------|
| `src/library/core/Security/KeyOptions.cs` | `KeyRelation`, `Relate`, `Label`, `RelationText`, `DescribeIssuer`; `Describe` rewritten |
| `src/test/KeyOptionsTest.cs` | 15 new tests for relations, labels and wording |
| `src/web/Pages/CreateCertificateAuthority.razor` | Parent key line; each radio shows its label and an outlined neutral chip |
| `src/web/Pages/CreateCertificateAuthority.razor.cs` | Label and relation helpers |

## Key Implementation Details

1. `Relate(issuer, candidate)` returns `SameAsParent`, `SameSecurityLevel` or `Stronger`, or null when weaker or unreadable. It uses the same security-bit comparison as the rule.
2. Labels include the level: "RSA 3072 bit - 128-bit security". RSA 3072 and 4096 therefore read as equal.
3. The note names the RSA 3072 / RSA 4096 example only at the 128-bit level.
4. The wording lives in `core`, so the leaf page reuses it.

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Rule | Unchanged | Agreed: compare security bits only |
| Chip style | Outlined, default color, no icon | Nothing should look like a warning |
| Where the text lives | `core` | Unit tested; shared with the leaf page |

## How to Verify

```bash
dotnet test ./src/test/ --filter "FullyQualifiedName~KeyOptionsTest"
dotnet build ./src/web
```

Then walk the UAT checklist in the test report in Visual Studio.
