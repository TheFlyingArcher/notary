---
run: run-specs-md-fire-planner-fad6e4-001
intent: hierarchical-ca-ordering
status: in_progress
---

# Test Report

## Work Item: ca-hierarchy-ordering

### Test Results
- Passed: 318 (full suite, includes 8 new)
- Failed: 0
- Skipped: 0

### Acceptance Criteria Validation
- ✅ Output is depth-first: each CA is followed by its descendants — `Flatten_IsDepthFirstWithChildrenUnderParent`
- ✅ Siblings and roots sorted by name — `Flatten_SortsRootsAndSiblingsByNameIgnoringCase`
- ✅ Multi-level chains give increasing depth — `Flatten_MultiLevelChain_IncreasesDepth`
- ✅ Missing or empty parent is a root — `Flatten_MissingParent_TreatedAsRoot`, `Flatten_EmptyParentSlug_TreatedAsRoot`
- ✅ Cycles cannot loop or hide a CA — `Flatten_SelfParent_*`, `Flatten_ParentCycle_*`

Command: `dotnet test src/test/Notary.Test.csproj`

---

## Work Item: create-root-ca-form

### Test Results
- Passed: 318 (full suite), Failed: 0, Skipped: 0
- Web project builds cleanly.

### Acceptance Criteria Validation
- ✅ Page title reads Create Root CA in root mode (`PageTitle`); verified by build and code review only
- ✅ Parent and purpose selectors hidden; Purpose set to Root; Is Issuer off via `OnPurposeChanged`
- ✅ Submit path unchanged; server-side Root rules covered by `CaPurposeIssuanceTest` (including Root with a parent rejected)
- ⚠️ Not exercised in a running UI: it needs MongoDB (Aspire/Docker), which is unavailable locally. Please confirm in Visual Studio.

---

## Work Item: create-intermediate-ca-under-parent

### Test Results
- Passed: 318 (full suite), Failed: 0, Skipped: 0
- Web project builds cleanly.

### Acceptance Criteria Validation
- ✅ Parent selector replaced by a read-only Issuing CA line (build/code review)
- ✅ Purpose dropdown excludes Root and still disables purposes the parent profile cannot issue
- ✅ Unknown slug, or a parent that cannot issue CAs, shows an error alert with a link back and no form
- ✅ No parameter and no root flag keeps the existing form; `?root=true` wins over `?parent=`
- ✅ Server-side enforcement untouched
- ⚠️ Not exercised in a running UI (needs MongoDB). Please confirm in Visual Studio.

---

## Work Item: hierarchical-ca-table

### Test Results
- Passed: 318 (full suite), Failed: 0, Skipped: 0
- Web project builds; the MudBlazor icon constants (Http, AssignmentInd, Email, Devices, WifiPassword, Tune, Add) all resolve.

### Acceptance Criteria Validation
- ✅ Rows depth-first with Name indented 24px per level (uses `CaHierarchy.Flatten`, covered by `CaHierarchyTest`)
- ✅ Button reads Create Root CA and links to `/ca/create?root=true`; empty-state text updated
- ✅ Round + button on rows whose CA can issue CAs, NotaryAdmin only, linking to `?parent=`
- ✅ Purpose icons per the issue; legacy CAs with a parent use Tune; roots show none
- ✅ Existing Details and Download buttons kept
- ⚠️ Not verified in a running UI (needs MongoDB). Please confirm in Visual Studio.
