# Walkthrough: Issuer Tree beside Issued Certificates

On CA pages the Issuer Tree (md=4) and Issued Certificates (md=8) share a grid; roots have no tree so the table is md=12. Leaf pages show the tree alone. The tree is a `RenderFragment` defined once in `CertificateDetail.razor`, and its heading now sits above the paper like the other panels.

## Verify

1. Intermediate CA: tree left (about a third), table right; narrow the window and they stack.
2. Root CA: table full width, no tree.
3. Leaf page: tree alone, no table.
