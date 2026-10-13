---
id: ca-purpose-selector-ui
title: Create CA purpose selector
intent: ca-purpose-profiles
complexity: medium
mode: confirm
status: completed
depends_on:
  - ca-purpose-profile-catalog
  - ca-purpose-persistence
  - ca-cert-from-profile
created: 2026-10-13T00:00:00Z
run_id: run-specsmd-fire-528fef-008
completed_at: 2026-10-13T02:25:40.461Z
---

# Work Item: Create CA purpose selector

## Description

Add a purpose selector to the Create CA page with a short description per purpose. Custom is visibly marked as discouraged. Root is disabled when a parent is chosen, and choices for a child are limited to those compatible with the parent's profile.

## Acceptance Criteria

- [ ] All seven purposes are presented with descriptions; Custom shows a discouraged marker
- [ ] Root is disabled when a parent CA is selected
- [ ] With a purpose parent, incompatible purposes are disabled with a reason
- [ ] The selected purpose is saved with the CA
- [ ] Verified in the running UI

## Technical Notes

src/web/Pages/CreateCertificateAuthority.razor(.cs) and its view model. Follow the validity dropdown pattern (disabled items with reasons, refresh on parent change).

## Dependencies

- ca-purpose-profile-catalog
- ca-purpose-persistence
- ca-cert-from-profile
