---
id: self-signed-certificate-form
title: Self-signed certificate form
intent: organize-certificates-by-ca
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-13T00:00:00Z
---

# Work Item: Self-signed certificate form

## Description

Add a dedicated self-signed form at /certificates/create-self-signed with the advanced parameters (subject, validity, key algorithm, usages, SANs), reusing the existing form logic.

## Acceptance Criteria

- [ ] The form issues a self-signed certificate with the advanced options previously available via the Self Signed option
- [ ] Advanced usage pickers and key algorithm settings are editable here
- [ ] Authorization matches Create Certificate (NotaryAdmin, NotaryWriter)
- [ ] Verified in the running UI

## Technical Notes

Prefer extracting shared form pieces over duplicating CreateCertificate.razor. No dependency on CA scoping, but must land with or before its removal from the CA form.

## Dependencies

(none)
