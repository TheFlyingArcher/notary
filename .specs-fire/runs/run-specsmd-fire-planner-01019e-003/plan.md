---
run: run-specsmd-fire-planner-01019e-003
work_item: create-certificate-scoped-to-ca
intent: organize-certificates-by-ca
mode: confirm
checkpoint: plan
approved_at: 2026-10-14T00:05:00Z
---

# Implementation Plan: Create Certificate scoped to the selected CA

## Approach

`/certificates/create?ca={slug}` receives the CA through a query parameter (`[SupplyParameterFromQuery(Name = "ca")]`) and the "Select CA" dropdown goes away.

1. On load, find the CA among `GetLeafIssuingAuthoritiesAsync()` results. That list already excludes revoked CAs, non-issuers and CAs that cannot issue leaf certificates. Missing, unknown or ineligible `ca` redirects to `/certificates` with a snackbar message.
2. Apply the CA once: subject fields, key algorithm/size/curve, usages locked or defaulted from its profile, validity capped at its expiry (the existing handler logic moved into one `ApplyCa` method).
3. Replace the dropdown with a read-only "Issuing CA" field showing the CA name and purpose. The form renders only after the CA is applied.
4. Remove the now-dead pieces: the `self` option, `OnCaListChange`, the slug-changed event and `CertificateAuthoritySlug` on the view model, `CaLabel`, and the "CA never issues leaf" warning branch.
5. Submit is unchanged: it still sets `ParentCertificateSlug` from the CA, and server-side enforcement is untouched.

## Heads-up on sequencing

Removing the Self Signed option means a self-signed certificate cannot be created from the UI until `self-signed-certificate-form` and the buttons item land. The existing self-signed logic is in git history for that item to reuse. Everything is on one branch, so nothing ships in between.

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/Pages/CreateCertificate.razor` | Read-only CA field instead of dropdown; render after CA applied; drop dead warning branch |
| `src/web/Pages/CreateCertificate.razor.cs` | Query parameter, eligibility check and redirect, `ApplyCa`, removal of dead code |
| `src/web/ViewModels/CreateCertificateViewModel.cs` | Remove `CertificateAuthoritySlug` and its event |

## Tests

| Test File | Coverage |
|-----------|----------|
| (none) | Page-only change; eligibility rules are already covered by the CA listing tests. Verified by build plus your UAT. |
