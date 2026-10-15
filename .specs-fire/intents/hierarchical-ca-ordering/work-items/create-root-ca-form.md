---
id: create-root-ca-form
title: Create Root CA form
intent: hierarchical-ca-ordering
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-14T00:00:00Z
run_id: run-specs-md-fire-planner-fad6e4-001
completed_at: 2026-10-14T23:29:38.453Z
---

# Work Item: Create Root CA form

## Description

Root mode of /ca/create (for example ?root=true) hides the parent selector and purpose dropdown and fixes the purpose to Root.

## Acceptance Criteria

- [ ] Page title reads Create Root CA
- [ ] Parent and purpose selectors are not shown; Purpose is Root
- [ ] Submitting creates a self-signed Root CA with existing key, validity and subject handling
- [ ] Test covers the root creation path

## Technical Notes

src/web/Pages/CreateCertificateAuthority.razor(.cs) and its view model. Keep Is Issuer behavior for Root unchanged.

## Dependencies

(none)
