---
id: leaf-profile-enforcement
title: Enforce issuing CA profile on leaf certificates
intent: ca-purpose-profiles
complexity: medium
mode: confirm
status: completed
depends_on:
  - ca-purpose-persistence
created: 2026-10-13T00:00:00Z
run_id: run-specsmd-fire-528fef-009
completed_at: 2026-10-13T03:12:11.468Z
---

# Work Item: Enforce issuing CA profile on leaf certificates

## Description

Reject a certificate request whose key usages or EKUs fall outside the issuing CA's profile. Self-signed certificates and legacy (Custom) CAs are unaffected.

## Acceptance Criteria

- [ ] Leaf with usages outside the issuing CA's allowed set returns Invalid before key generation
- [ ] Leaf within the allowed set succeeds
- [ ] A Root CA never issues leaf certificates: any non-CA request with a Root issuer returns Invalid (confirmed by the user at the catalog design review)
- [ ] Self-signed certificates are unaffected
- [ ] A legacy CA with no purpose behaves as Custom (unrestricted)
- [ ] Tests over real certificates cover each case

## Technical Notes

CertificateService.IssueCertificateAsync already loads the parent certificate; the issuing CA's purpose is looked up from it. Follows the validity enforcement pattern.

## Dependencies

- ca-purpose-persistence
