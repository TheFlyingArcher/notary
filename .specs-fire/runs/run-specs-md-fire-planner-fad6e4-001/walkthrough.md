---
run: run-specs-md-fire-planner-fad6e4-001
intent: hierarchical-ca-ordering
scope: wide
completed: 2026-10-14T23:32:46.906Z
---

# Walkthrough: Hierarchical ordering of CAs (issue 40)

## What changed

The `/ca` page now lists certificate authorities as a hierarchy: each CA is followed by the CAs it issued, indented by depth, in the same MudTable. Root CAs and intermediate CAs are created from dedicated entry points instead of one general form.

## Pieces

| Piece | Where | Behavior |
|-------|-------|----------|
| Ordering | `src/library/core/CaHierarchy.cs`, `Contract/CaHierarchyRow.cs` | `Flatten` returns depth-first rows with depth. Roots and siblings sort by name. A CA whose parent is missing is a root; a parent cycle cannot loop or hide a CA. |
| Root form | `/ca/create?root=true` | Hides the parent and purpose selects, fixes Purpose to Root, heading "Create Root CA". |
| Intermediate form | `/ca/create?parent={slug}` | Parent locked and shown read-only; subject, key and validity cap inherited through the existing parent handler; Root removed from the purpose list. Unknown slug or a parent that cannot issue CAs shows an error with a link back. `?root=true` wins if both are given. |
| Table | `CertificateAuthorities.razor(.cs)` | Indented names, purpose icon with tooltip, round + button (NotaryAdmin only, only where the CA can issue CAs), "Create Root CA" button. Details and Download are the existing buttons. |

Icons: TLS/SSL Http, User login AssignmentInd, S/MIME Email, IoT Devices, 802.1x/RADIUS WifiPassword, Custom Tune. A legacy CA with no purpose uses Tune when it has a parent; roots show no icon.

## Decisions

- Details and Download were reused, not rebuilt: `/ca` already had both on every row.
- No component test harness exists, so page logic stays thin and the testable part (ordering) lives in the library.
- The + button is a convenience; the server still enforces who may create a CA and under which parent.

## Deviations from plan

None.

## Verify (Visual Studio)

1. Open `/ca` with a root, two intermediates, and one intermediate under an intermediate: confirm order, indentation and icons.
2. Click **Create Root CA**: no parent or purpose dropdowns; submit creates a Root.
3. Click + on a root and on an intermediate: parent is locked, Root is not offered, subject/validity inherit.
4. Browse to `/ca/create?parent=bogus`: error with a link back. A parent that issues end-entity certificates only (e.g. TLS CA under a parent, whose + should not appear) shows the error if opened directly.
5. `/ca/create` without parameters still shows the original form.

## Tests

8 new (`CaHierarchyTest`); full suite 318 passed. The UI could not be run locally (needs MongoDB), so steps above are unverified.
