---
run: run-specsmd-fire-528fef-011
work_item: custom-ca-usages-ui
intent: ca-purpose-profiles
mode: confirm
checkpoint: plan
approved_at: pending
---

# Implementation Plan: Custom CA usage pickers

## Approach

When Custom is the chosen purpose, the Create CA page shows pickers for the usages the CA will carry. They appear only for Custom, under the existing discouraged warning. The choices are saved on the CA and used to build its certificate (the service already does this).

**Page behavior (Custom only)**
- A fixed line: "Always included: Certificate Key Sign, CRL Sign". These two are in every CA certificate, because a CA cannot sign certificates or publish revocations without them.
- A multi-select **Additional Key Usages** for the other key usages (Digital Signature, Key Encipherment, and so on).
- A multi-select **Extended Key Usages**, listing every known usage with readable names.
- Under a parent that restricts extended key usages (every purpose CA, or a Custom CA that chose some): options outside the parent's set are disabled with a reason, and at least one extended key usage must be chosen, with a clear message. Choices are cleared automatically if you change to a parent that does not allow them.
- On submit, the CA is saved with `CustomKeyUsages` (the two fixed ones plus the extras) and `CustomExtendedKeyUsages`. Sending the two fixed usages explicitly is what tells the service this is a deliberate Custom choice and not the old "no purpose" case, so a Custom CA with nothing extra chosen gets only the two key usages and no extended key usage, instead of silently receiving the previous broad set.
- The Custom note changes from "usages cannot be chosen yet" to explaining that the choices below define the CA and that a mistake can weaken it or break trust chains.

**Core**
- Custom becomes selectable under a restricted parent. It was disabled only because the pickers did not exist. The "must list explicit usages" rule is now met by the required-extended-key-usage message on the page and still enforced by the server. `EvaluateForParent` therefore only requires the parent to be able to issue CAs for Custom.
- `KeyPurposeOids.Known`: the list of known extended key usages with names, used by the picker and by `GetName`.
- `CaProfileCatalog.DescribeKeyUsage` made public, so the picker and the error messages name key usages identically.

## One change to the work item
The criterion "at least one key usage is required" is met by the two fixed usages, which are always present, so no separate rule is needed.

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/core/Profiles/KeyPurposeOids.cs` | Add `Known` list; `GetName` uses it |
| `src/library/core/Profiles/CaProfileCatalog.cs` | Custom allowed under a restricted parent; `DescribeKeyUsage` public |
| `src/web/ViewModels/CreateCertificateAuthorityViewModel.cs` | Selected custom key usages and extended key usages |
| `src/web/Pages/CreateCertificateAuthority.razor` | Pickers for Custom, fixed-usages line |
| `src/web/Pages/CreateCertificateAuthority.razor.cs` | Option rules under a parent, clearing, validation, save, updated Custom note |
| `src/test/CaProfileCatalogTest.cs` | Update Custom expectations; new tests |
| `src/test/CaPurposeIssuanceTest.cs` | Real-certificate cases for Custom |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/CaProfileCatalogTest.cs` | `EvaluateForParent` now allows Custom under TLS, IoT and Custom parents and still disables everything under a path-length-0 parent; `Known` includes the four profile usages and the rest with names; `GetName` and `DescribeKeyUsage` |
| `src/test/CaPurposeIssuanceTest.cs` | Custom CA created the way the page will: the two fixed key usages sent explicitly and no extended key usage gets exactly those two key usages and no extended key usage extension (not the broad legacy set); extras are added; Custom under a TLS/SSL parent with Server Authentication succeeds and with Client Authentication, or with none, is rejected |

The page cannot be run here, so it is for your UAT. I will compile-check the web project by building it to a temporary folder.

---
*Plan approved at checkpoint. Execution follows.*
