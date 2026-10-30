# Walkthrough: Info and CRL side by side

The CRL panel item moved into the Info panel grid in `CertificateDetail.razor`, guarded by `IsCaCertificate`. On CAs both items are `xs=12 md=6`; on leaf certificates the Info item stays `xs=6`.

## Verify

1. CA page: Info and CRL side by side; narrow the window and they stack.
2. Leaf page: Status panel unchanged at half width.
