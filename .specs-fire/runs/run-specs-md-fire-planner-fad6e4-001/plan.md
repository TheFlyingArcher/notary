---
run: run-specs-md-fire-planner-fad6e4-001
work_item: ca-hierarchy-ordering
intent: hierarchical-ca-ordering
mode: confirm
checkpoint: plan
---

# Implementation Plan: CA hierarchy ordering

## Approach

Add a small pure helper, `CaHierarchy.Flatten`, in the core library. It takes the flat CA list and returns rows in depth-first order. Each row is a positional record of the CA and its depth. Roots and siblings are sorted by name, ordinal and case-insensitive. A CA whose `ParentCaSlug` is empty or missing from the list is treated as a root. A visited set stops parent cycles, and any CA left unreachable by a cycle is appended as a root so it never disappears. Iterative traversal, no recursion.

## Files to Create

| File | Purpose |
|------|---------|
| `src/library/core/Contract/CaHierarchyRow.cs` | Positional record `CaHierarchyRow(CertificateAuthority Authority, int Depth)` |
| `src/library/core/CaHierarchy.cs` | Static `Flatten(IEnumerable<CertificateAuthority>)` helper |
| `src/test/CaHierarchyTest.cs` | NUnit tests |

## Files to Modify

| File | Changes |
|------|---------|
| (none) | |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/CaHierarchyTest.cs` | depth-first order, name-sorted roots/siblings, multi-level depth, orphan as root, cycle guard, empty input |

---

## Work Item: create-root-ca-form

### Approach

Add a root mode to `/ca/create`, selected by the query parameter `?root=true`. In that mode the page sets the parent to "No Parent" and the purpose to Root during initialization, hides the "Select Parent CA" and "CA Purpose" selectors, and shows the heading "Create Root CA". `OnPurposeChanged` already turns Is Issuer off for Root, and the existing alert explains what a Root CA does. Without the parameter the page is unchanged. Submit is unchanged, so the server-side Root rules still apply.

### Files to Create

(none)

### Files to Modify

| File | Changes |
|------|---------|
| `src/web/Pages/CreateCertificateAuthority.razor` | Heading by mode; hide parent and purpose selects in root mode |
| `src/web/Pages/CreateCertificateAuthority.razor.cs` | `[SupplyParameterFromQuery] root` parameter, root-mode initialization |

### Tests

The test project has no Blazor component harness, so the page is verified by a clean build and in the running UI. Server-side Root creation is already covered by the existing `CaPurposeIssuanceTest` / `CertificateAuthorityServiceTest`; I'll confirm that coverage and add a test only if a gap exists.

---

## Work Item: create-intermediate-ca-under-parent

### Approach

Add an intermediate mode to `/ca/create`, selected by `?parent={slug}` (`?root=true` wins if both are given). After the CA list loads, the page looks the slug up and checks `CaProfileCatalog.CheckCanIssueCas`. If the slug is unknown or that CA cannot issue CAs, the page shows an error alert with a link back to `/ca` and no form. Otherwise it sets `ViewModel.ParentCaSlug`, which reuses the existing handler to load the parent and copy its subject, key settings and validity cap. The "Select Parent CA" dropdown is replaced by a read-only "Issuing CA" line. The CA Purpose dropdown stays, minus the Root option. The heading reads "Create Intermediate CA". With neither parameter the form is unchanged.

### Files to Create

(none)

### Files to Modify

| File | Changes |
|------|---------|
| `src/web/Pages/CreateCertificateAuthority.razor` | Error alert, read-only issuing CA line, mode-based heading |
| `src/web/Pages/CreateCertificateAuthority.razor.cs` | `[SupplyParameterFromQuery] parent`, parent lookup and validation, purpose options without Root |

### Tests

Same limit as the previous item: no component harness, so a clean build plus full suite, and a UI check in Visual Studio. Server-side parent and purpose enforcement is covered by `CaPurposeIssuanceTest`.

---

## Work Item: hierarchical-ca-table

### Approach

`/ca` keeps its single `MudTable` but binds it to `CaHierarchy.Flatten(CaList)`. The Name cell is indented by 24px per depth level and shows the purpose icon first: TLS/SSL `Http`, User login `AssignmentInd`, S/MIME `Email`, IoT `Devices`, 802.1x/RADIUS `WifiPassword`, Custom `Tune`, with the purpose name as a tooltip. A legacy CA with no purpose uses `Tune` when it has a parent; a root (Root purpose, or no purpose and no parent) shows no icon. The header button becomes **Create Root CA**, linking to `/ca/create?root=true`, and the empty-state text is updated. Each row whose CA can issue CAs (`CaProfileCatalog.CheckCanIssueCas`) gets a round `+` button linking to `/ca/create?parent={slug}`, shown to NotaryAdmin only, like the create button. The existing Details and Download buttons stay on every row.

### Files to Create

(none)

### Files to Modify

| File | Changes |
|------|---------|
| `src/web/Pages/CertificateAuthorities.razor` | Ordered rows, indentation, icons, `+` button, Create Root CA button, empty-state text |
| `src/web/Pages/CertificateAuthorities.razor.cs` | `CaRows`, purpose-to-icon mapping, can-add-intermediate check |

### Tests

Ordering is already covered by `CaHierarchyTest`. The page itself has no component harness, so: clean build, full suite, and a check in Visual Studio. Icon constant names are confirmed by the build.
