---
run: run-specsmd-fire-planner-01019e-004
work_item: self-signed-certificate-form
intent: organize-certificates-by-ca
mode: confirm
checkpoint: plan
approved_at: 2026-10-14T00:30:00Z
---

# Implementation Plan: Self-signed certificate form

## Approach

A new page at `/certificates/create-self-signed` restores the self-signed path removed from Create Certificate, as its own form with the advanced parameters. It is the previous self-signed behavior of Create Certificate, not new behavior.

- Same authorization as Create Certificate (`NotaryAdmin,NotaryWriter`).
- Reuses `CreateCertificateViewModel` and `SubjectAlternativeNameDialog`.
- Editable fields: name, common name, validity (`ValidityPolicy.Evaluate(Leaf, now, null)`, no issuer cap), extended and certificate key usages (defaults from `ResetUsages`, at least one key usage required), additional subject properties, SANs, private key algorithm, RSA size or curve.
- Submit builds the `CertificateRequest` with no `ParentCertificateSlug`, calls `IssueCertificateAsync`, shows a snackbar on failure and navigates to `certificates` on success.
- To limit duplication, the two name validators move to a small shared static class used by both pages. The rest of the form markup is deliberately not extracted into a shared component here: that is a larger refactor of a page I cannot run in this environment, so I'd rather keep the working scoped-create page untouched and consider a shared component later.

No navigation entry is added; the buttons work item links to this page.

## Files to Create

| File | Purpose |
|------|---------|
| `src/web/Pages/CreateSelfSignedCertificate.razor` | Self-signed form markup |
| `src/web/Pages/CreateSelfSignedCertificate.razor.cs` | Form logic and submit |
| `src/web/Shared/CertificateFormRules.cs` | Shared name and common-name validators |

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/Pages/CreateCertificate.razor.cs` | Use the shared validators |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/CertificateIssuanceSelfSignedTest.cs` (only if the service lacks one) | A request with no parent issues a self-signed, non-CA certificate with the chosen usages |

I'll check existing issuance tests first and only add this one if self-signed leaf issuance isn't already covered. The page itself is verified by build, then your UAT (deferred to the end as you asked).
