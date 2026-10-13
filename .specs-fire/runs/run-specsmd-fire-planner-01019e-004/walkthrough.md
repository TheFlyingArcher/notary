---
run: run-specsmd-fire-planner-01019e-004
work_item: self-signed-certificate-form
intent: organize-certificates-by-ca
generated: 2026-10-14T00:50:00Z
mode: confirm
---

# Implementation Walkthrough: Self-signed certificate form

## Summary

`/certificates/create-self-signed` is a dedicated form for self-signed certificates with the advanced parameters (validity, usages, subject details, SANs, key algorithm). It restores what the Self Signed option of Create Certificate did, now on its own page. It is reachable by URL until the buttons item links it.

## Files Changed

| File | Change |
|------|--------|
| `src/web/Pages/CreateSelfSignedCertificate.razor(.cs)` | New form; request has no parent certificate |
| `src/web/Shared/CertificateFormRules.cs` | Shared name and common-name validators |
| `src/web/Pages/CreateCertificate.razor(.cs)` | Uses the shared validators |
| `src/test/CaPurposeIssuanceTest.cs` | Self-signed leaf test |

## How to Verify (UAT, deferred)

Open `/certificates/create-self-signed`, fill name, common name and a key usage, submit: the certificate appears under Self-signed on the Certificates page.
