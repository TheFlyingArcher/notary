---
run: run-specsmd-fire-planner-01019e-003
work_item: create-certificate-scoped-to-ca
intent: organize-certificates-by-ca
generated: 2026-10-14T00:20:00Z
mode: confirm
---

# Implementation Walkthrough: Create Certificate scoped to the selected CA

## Summary

`/certificates/create?ca={slug}` now issues from that CA only. The CA must be in the list of non-revoked, leaf-issuing CAs; otherwise the user is sent back to `/certificates` with a message. The CA's subject, key settings, usages and validity cap are applied once on load, and the Select CA dropdown is replaced by a read-only Issuing CA field.

## Files Changed

| File | Changes |
|------|---------|
| `src/web/Pages/CreateCertificate.razor` | Read-only Issuing CA field; form shown after the CA is applied; removed the unreachable warning branch |
| `src/web/Pages/CreateCertificate.razor.cs` | `ca` query parameter, eligibility check and redirect, `ApplyCa`; removed dropdown handlers |
| `src/web/ViewModels/CreateCertificateViewModel.cs` | Removed `CertificateAuthoritySlug` and its change event |

## How to Verify (UAT)

1. Open `/certificates/create?ca=<slug of a TLS/other leaf-issuing CA>`: no CA dropdown, Issuing CA shown read-only, subject and key fields filled and locked, usages follow the CA profile.
2. Submit: the certificate is issued by that CA.
3. Open `/certificates/create`, or with an unknown, revoked or Root CA slug: redirected to Certificates with a warning.

Self-signed creation has no entry point until the next work items.
