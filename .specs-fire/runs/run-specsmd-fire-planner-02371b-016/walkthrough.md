# Walkthrough: Hide the issuer tree on root CAs

Run: run-specsmd-fire-planner-02371b-016 · Work item: ca-detail-hide-root-tree

The Issuer Tree panel is wrapped in `@if (!Model.IsRootCa)`, and `PopulateIssuerTree` is skipped for roots. `IsRootCa` comes from the previous work item and is only set for CAs, so leaf certificates and intermediate CAs keep the tree.

## Verify

1. Open a root CA: no Issuer Tree panel.
2. Open an intermediate CA: the tree shows its issuers.
3. Open a leaf certificate: the tree is unchanged.
