---
id: certificates-page-create-buttons
title: Create buttons on the Certificates page
intent: organize-certificates-by-ca
complexity: low
mode: autopilot
status: pending
depends_on: [certificates-ca-dropdown-ui, create-certificate-scoped-to-ca, self-signed-certificate-form]
created: 2026-10-13T00:00:00Z
---

# Work Item: Create buttons on the Certificates page

## Description

Wire the Certificates page buttons: Create Certificate (for the selected CA, disabled for Self-signed or with no CA) and Create Self-Signed Certificate. Keep the existing role-based disabled state.

## Acceptance Criteria

- [ ] Create Certificate links to /certificates/create?ca={selected slug}
- [ ] Create Self-Signed Certificate links to the self-signed form
- [ ] Both are disabled for users without NotaryAdmin/NotaryWriter
- [ ] End-to-end flow verified in the running UI

## Technical Notes

src/web/Pages/Certificates.razor. Final integration step.

## Dependencies

- certificates-ca-dropdown-ui
- create-certificate-scoped-to-ca
- self-signed-certificate-form
