---
id: validity-server-enforcement
title: Server-side issuer validity enforcement
intent: validity-period-dropdown
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-12T00:00:00Z
run_id: run-specsmd-fire-528fef-002
completed_at: 2026-10-13T01:12:52.559Z
---

# Work Item: Server-side issuer validity enforcement

## Description

Make CertificateService and CertificateAuthorityService reject issuing a certificate or child CA whose validity extends beyond its issuing CA, so the UI is not the only guard.

## Acceptance Criteria

- [ ] Issuing a leaf with NotAfter later than its issuer's NotAfter fails with a Result error, not an exception
- [ ] Creating a child CA with NotAfter later than its parent's fails the same way
- [ ] A child with NotBefore earlier than its issuer's NotBefore is rejected
- [ ] Self-signed roots (no parent) are unaffected
- [ ] Callers surface the error message to the user
- [ ] Unit tests cover rejection, acceptance at the exact boundary, and the self-signed case

## Technical Notes

Touch points: CertificateService (parent lookup near ParentCertificateSlug, ~line 57) and CertificateAuthorityService. Check how existing issuance errors flow to callers before choosing the error shape.

## Dependencies

(none)
