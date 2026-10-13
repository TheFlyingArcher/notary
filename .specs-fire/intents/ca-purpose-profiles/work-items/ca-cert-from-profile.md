---
id: ca-cert-from-profile
title: Issue CA certificates from the purpose profile
intent: ca-purpose-profiles
complexity: high
mode: validate
status: completed
depends_on:
  - ca-purpose-profile-catalog
  - ca-purpose-persistence
created: 2026-10-13T00:00:00Z
run_id: run-specsmd-fire-528fef-007
completed_at: 2026-10-13T02:14:38.185Z
---

# Work Item: Issue CA certificates from the purpose profile

## Description

Build the CA certificate from the selected purpose's profile instead of the hardcoded broad usages, including EKUs on the CA certificate and a basicConstraints path length. Reject a child CA outside its parent's profile.

## Acceptance Criteria

- [ ] CA certificate key usage, EKUs and path length come from the profile (or the Custom selection)
- [ ] basicConstraints supports a path length, and a path length of zero or more is emitted for purpose CAs
- [ ] A child CA outside its parent's profile is rejected with an Invalid Result before any key is generated
- [ ] Root carries keyCertSign and cRLSign only, with no EKU restriction
- [ ] Real-certificate tests parse the issued CA certificate and assert KU, EKU and pathLen per purpose

## Technical Notes

CertificateAuthorityService.CreateAsync and CertificateService.GenerateCertificate / AddBasicConstraints (no pathLen support today). Security-sensitive: signing and CA certificate contents.

## Dependencies

- ca-purpose-profile-catalog
- ca-purpose-persistence
