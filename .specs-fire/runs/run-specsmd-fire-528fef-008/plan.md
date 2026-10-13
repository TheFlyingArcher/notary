---
run: run-specsmd-fire-528fef-008
work_item: ca-purpose-selector-ui
intent: ca-purpose-profiles
mode: confirm
checkpoint: plan
approved_at: pending
---

# Implementation Plan: Create CA purpose selector

## Approach

Add the purpose selector to the Create CA page. The option rules live in the core library so they are unit-tested and shared with the service; the page only displays them.

**Core (testable logic)**
- `CaProfileCatalog.Resolve(CertificateAuthority)`: the profile for a stored CA (named purpose, Custom with chosen usages, or Custom/unrestricted when none). This is the `ResolveProfile` helper now private in `CertificateAuthorityService`, moved here.
- `CaProfileCatalog.CheckIssuer(CertificateAuthority parent, CaProfile child)`: `CheckChild` plus the rule that a purpose CA with its own parent (path length 0) cannot issue CAs. `CertificateAuthorityService` calls this in place of its inline checks, so the page and the service cannot disagree.
- `CaProfileCatalog.EvaluateForParent(CertificateAuthority? parent)`: the seven purposes as `CaPurposeOption(Profile, IsAllowed, DisabledReason)`. With no parent everything is allowed; with a parent each option comes from `CheckIssuer`, so Root is disabled ("cannot be issued by another CA"), purposes outside the parent's usages are disabled with the reason, and Custom is disabled under a restricted parent until the Custom usage pickers exist (`custom-ca-usages-ui`).

**Create CA page**
- A required purpose select above the validity field, no default, so a purpose is chosen deliberately. Each item shows the name and a one-line description. Disabled items show the reason. Custom is labeled "(discouraged)".
- Once chosen, a short summary shows what the CA will allow in plain words (for example "Server Authentication"), and that a purpose CA with a parent issues end-entity certificates only. Custom shows a warning that it is discouraged. Root shows that it signs intermediate CAs only.
- "Is Issuer" is set automatically: off and locked for Root (it never issues certificates), on by default for other purposes (the user can change it). The Create Certificate page only lists CAs marked Is Issuer, so a TLS/SSL CA must have it on to issue certificates.
- In the parent list, CAs that cannot issue other CAs (a purpose CA that has its own parent) are disabled with a short note, and each name shows its purpose (existing CAs show "Legacy").
- Changing the parent re-evaluates the options and clears a purpose that no longer fits, alongside the existing validity refresh.
- Submit sets `Purpose`. Existing validity behavior is unchanged: a CA with no parent uses the root period list, one with a parent the intermediate list.

**Until the Custom pickers item lands:** choosing Custom saves a Custom CA with no chosen usages, which the service issues with the previous broad usages. The page says so.

## Files to Create

| File | Purpose |
|------|---------|
| `src/library/core/Profiles/CaPurposeOption.cs` | Option record for the selector |

## Files to Modify

| File | Changes |
|------|---------|
| `src/library/core/Profiles/CaProfileCatalog.cs` | Add `Resolve`, `CheckIssuer`, `EvaluateForParent` |
| `src/library/service/CertificateAuthorityService.cs` | Use `Resolve` and `CheckIssuer` instead of private duplicates |
| `src/web/ViewModels/CreateCertificateAuthorityViewModel.cs` | Add `SelectedPurpose` |
| `src/web/Pages/CreateCertificateAuthority.razor` | Purpose select, summary and warnings, Is Issuer handling, parent list notes |
| `src/web/Pages/CreateCertificateAuthority.razor.cs` | Options, refresh on parent change, set `Purpose` on submit |
| `src/test/CaProfileCatalogTest.cs` | Tests for the new methods |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/CaProfileCatalogTest.cs` | `Resolve` for null, named, Custom with and without usages; `CheckIssuer` including the path length 0 rule and Root parent; `EvaluateForParent` with no parent (all seven allowed), a Root parent (Root disabled, the rest allowed), a TLS parent (only TLS allowed, others disabled with reasons), a restricted Custom parent, and a path-length-0 parent (all disabled). The 18 existing issuance tests re-run to prove the service refactor changed nothing. |

## Technical Details

Same MudBlazor pattern as the validity dropdowns. The page cannot be run here (no MongoDB or login), so its behavior is for your UAT in Visual Studio; this item is the point where the real-chain UAT becomes possible. Verified with `dotnet build src/Notary.sln`, `dotnet test ./src/test/` and `dotnet format --verify-no-changes`.

---
*Plan approved at checkpoint. Execution follows.*
