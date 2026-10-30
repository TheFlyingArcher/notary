---
run: run-specsmd-fire-planner-02371b-019
work_item: ca-tree-beside-issued-table
intent: ca-detail-layout
mode: confirm
checkpoint: plan
---

# Implementation Plan: Issuer Tree beside Issued Certificates

## Approach

In `CertificateDetail.razor`, on CA pages put the Issuer Tree and the Issued Certificates table in one `MudGrid`:
- non-root CA: tree `xs="12" md="4"`, table `xs="12" md="8"`
- root CA: no tree, table `xs="12" md="12"`
Both stack full width below the tablet breakpoint.

Leaf certificate pages keep the tree in its own full-width block with no table. To avoid writing the tree markup twice, define it once as a Razor template (`RenderFragment issuerTree = @<...>`) and render it in the CA grid or in the leaf block.

For the two columns to line up, the tree's "Issuer Tree" heading moves from inside its paper to above it (heading, then a `pa-3` paper), matching the Info, CRL, Subject and Issuer panels, and the table heading above the table. Leaf pages get the same tree styling, so the only visible change there is the heading sitting above the tree paper like the other panels. The tree, its icons, expansion, the table columns, buttons, empty message and bottom spacer are otherwise unchanged.

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/Pages/CertificateDetail.razor` | Tree and table in one grid for CAs; tree defined once as a Razor template; heading above the tree paper |

## Tests

None: layout only. Verified by build, the existing suite and Visual Studio UAT.
