---
id: organize-certificates-by-ca
title: Organize certificates by issuing CA
status: completed
created: 2026-10-13T00:00:00Z
source: https://github.com/TheFlyingArcher/notary/issues/39
completed_at: 2026-10-13T23:45:23.110Z
---

# Intent: Organize certificates by issuing CA

## Goal

Organize the Certificates page by issuing CA, and scope certificate creation to the CA the user selected.

## Users

Notary operators (NotaryAdmin, NotaryWriter create; read-only roles see the same filtered list).

## Problem

`/certificates` lists every certificate in one flat table with no indication of which CA issued it. This gets messy as the count grows.

## Success Criteria

- `/certificates` has a dropdown of non-revoked CAs that issue leaf certificates, plus a "Self-signed" entry
- Selecting an entry lists only the certificates issued by that CA, or only the self-signed ones
- The last-used selection is remembered in the browser; the default is the first CA alphabetically
- An empty selection shows a message instead of an empty table, and a message is shown when no leaf-issuing CAs exist
- "Create Certificate" opens `/certificates/create?ca={slug}` with that CA locked and its profile, subject and validity cap applied; the "Select CA" dropdown is removed from that form
- A second button opens a self-signed form with the advanced parameters
- Existing server-side issuer and profile enforcement is unchanged

## Constraints

- Blazor/MudBlazor UI in src/web; reuse existing `GetCertificatesByCaAsync` where possible
- Roles and authorization behavior unchanged
- CA purpose profile rules still apply (CAs that cannot issue leaf certificates never appear)
- Old `/certificates/create` links without a `ca` parameter need a sensible fallback
- Out of scope: pagination, search, certificate storage changes

## Notes

Source: GitHub issue #39 (label: enhancement).
