---
id: certificates-ca-dropdown-ui
title: CA dropdown on the Certificates page
intent: organize-certificates-by-ca
complexity: medium
mode: confirm
status: completed
depends_on: [certificates-by-ca-listing]
created: 2026-10-13T00:00:00Z
---

# Work Item: CA dropdown on the Certificates page

## Description

Add the CA dropdown with a Self-signed entry to /certificates. The table shows only the selected scope. The last choice is remembered in the browser and defaults to the first CA alphabetically.

## Acceptance Criteria

- [ ] Dropdown lists leaf-issuing, non-revoked CAs plus Self-signed
- [ ] Selecting an entry filters the table to that scope
- [ ] Last selection persists across reloads; falls back to the first CA if it no longer exists
- [ ] An empty scope shows a message instead of an empty table; no leaf-issuing CAs shows a distinct message
- [ ] Verified in the running UI

## Technical Notes

src/web/Pages/Certificates.razor(.cs). Use browser local storage via the existing Blazor approach in the project, if any.

## Dependencies

- certificates-by-ca-listing
