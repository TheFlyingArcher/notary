---
run: run-specsmd-fire-planner-01019e-002
work_item: certificates-ca-dropdown-ui
intent: organize-certificates-by-ca
mode: confirm
checkpoint: plan
approved_at: 2026-10-13T23:45:00Z
---

# Implementation Plan: CA dropdown on the Certificates page

## Approach

Blazor Server page, so the remembered choice uses the built-in `ProtectedLocalStorage` (no new package), read in `OnAfterRenderAsync` because JS interop is unavailable during the first render.

1. Load leaf-issuing, non-revoked CAs with `GetLeafIssuingAuthoritiesAsync`.
2. Dropdown items: "Self-signed" first, then the CAs by name. The selected value is a CA slug or the literal `self`.
3. Initial selection: stored value if it is still in the list, else the first CA, else "Self-signed" when there are no CAs.
4. Table: CA selected, certificates from `GetCertificatesByCaAsync(ca.CertificateSlug)`; Self-signed, from `GetSelfSignedCertificatesAsync()`. CA certificates (intermediates) stay out of the list, as today. Changing the selection reloads the table and stores the choice.
5. Empty states: `NoRecordsContent` on the table says no certificates for this CA (or no self-signed certificates). An info alert says no certificate authorities can issue certificates yet when the CA list is empty.
6. The existing Create Certificate button is left as is; the buttons work item rewires it.

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/Pages/Certificates.razor` | CA dropdown, scoped table, empty-state messages |
| `src/web/Pages/Certificates.razor.cs` | Selection state, loading by scope, ProtectedLocalStorage persistence, inject `ICertificateAuthorityService` |

## Tests

| Test File | Coverage |
|-----------|----------|
| (none) | Page logic is UI-only; the queries are already tested. Verified by running the web app. |

## Technical Details

Local storage key `certificates.selectedCa`. Storage failures (unavailable or tampered payload) fall back to the default selection. Verification: build the web project and exercise the page in the browser; a MongoDB instance may be needed to see data, so I'll report what I could verify.
