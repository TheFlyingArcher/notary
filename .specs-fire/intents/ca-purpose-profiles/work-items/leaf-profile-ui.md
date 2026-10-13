---
id: leaf-profile-ui
title: Create Certificate follows the issuing CA profile
intent: ca-purpose-profiles
complexity: medium
mode: confirm
status: completed
depends_on:
  - leaf-profile-enforcement
created: 2026-10-13T00:00:00Z
run_id: run-specsmd-fire-528fef-010
completed_at: 2026-10-13T03:19:32.883Z
---

# Work Item: Create Certificate follows the issuing CA profile

## Description

On Create Certificate, a CA-issued certificate shows the issuing CA's allowed usages, preselected and read-only. Advanced usage pickers are available only for self-signed certificates.

## Acceptance Criteria

- [ ] Selecting an issuing CA locks the key usage and EKU pickers to its profile
- [ ] Switching to self-signed restores the editable advanced pickers
- [ ] Legacy (Custom) CAs behave as before
- [ ] Verified in the running UI

## Technical Notes

src/web/Pages/CreateCertificate.razor(.cs) and CreateCertificateViewModel (currently preselects TLS defaults).

## Dependencies

- leaf-profile-enforcement
