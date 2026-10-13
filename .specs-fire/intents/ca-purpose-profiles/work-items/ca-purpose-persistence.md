---
id: ca-purpose-persistence
title: Persist CA purpose
intent: ca-purpose-profiles
complexity: medium
mode: confirm
status: completed
depends_on:
  - ca-purpose-profile-catalog
created: 2026-10-13T00:00:00Z
run_id: run-specsmd-fire-528fef-006
completed_at: 2026-10-13T02:09:16.583Z
---

# Work Item: Persist CA purpose

## Description

Store the CA purpose on the CA record (contract and Mongo model, with mapping). For Custom, also store the chosen key usages and EKUs.

## Acceptance Criteria

- [ ] CertificateAuthority contract and CertificateAuthorityModel carry the purpose (and custom usages)
- [ ] Mapping round-trips all values
- [ ] Existing documents without a purpose load as Custom, no migration required
- [ ] Unit tests cover mapping and the legacy default

## Technical Notes

Mongo elements follow existing snake_case naming (see CertificateAuthorityModel). Model already tolerates unknown fields.

## Dependencies

- ca-purpose-profile-catalog
