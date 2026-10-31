---
id: create-certificate-scoped-to-ca
title: Create Certificate scoped to the selected CA
intent: organize-certificates-by-ca
complexity: medium
mode: confirm
status: completed
depends_on: [certificates-by-ca-listing]
created: 2026-10-13T00:00:00Z
---

# Work Item: Create Certificate scoped to the selected CA

## Description

Create Certificate takes the CA from a ca query parameter, locks it, and applies its profile, subject and validity cap. The Select CA dropdown and Self Signed option are removed from this form. Requests without a valid ca redirect back to /certificates.

## Acceptance Criteria

- [ ] /certificates/create?ca={slug} issues from that CA with the existing profile, subject and validity behavior
- [ ] The Select CA dropdown is gone; the CA name is shown read-only
- [ ] A missing, unknown, revoked or non-leaf-issuing ca redirects to /certificates with a message
- [ ] Verified in the running UI

## Technical Notes

src/web/Pages/CreateCertificate.razor(.cs), CreateCertificateViewModel. Server-side enforcement already exists and must keep working.

## Dependencies

- certificates-by-ca-listing
