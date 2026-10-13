---
id: ca-purpose-profile-catalog
title: CA purpose profile catalog
intent: ca-purpose-profiles
complexity: high
mode: validate
status: completed
depends_on: []
created: 2026-10-13T00:00:00Z
run_id: run-specsmd-fire-528fef-005
completed_at: 2026-10-13T02:05:52.658Z
---

# Work Item: CA purpose profile catalog

## Description

Add a pure, UI-agnostic library of CA purposes and their profiles: a CaPurpose enum (Root, TLS/SSL, User login, S/MIME, IoT devices, 802.1x/RADIUS, Custom), a positional CaProfile record (CA key usages, CA extended key usages, path length, and the key usages / EKUs its leaf certificates may carry), a catalog of the draft profiles, and a within-parent check.

## Acceptance Criteria

- [ ] Each purpose has a profile approved by the user in the design doc
- [ ] Root has no EKU restriction; each purpose CA carries only its purpose's EKUs
- [ ] A within-parent function returns Result: a child profile must be a subset of its parent's allowed usages; Root is never a child
- [ ] A missing/null purpose resolves to Custom (unrestricted); Custom has no path length constraint
- [ ] Root never issues leaf certificates (IssuesLeafCertificates is false for Root)
- [ ] Unit tests cover every profile, subset success/failure, Root-under-parent rejection and the Custom default

## Technical Notes

Library core, namespace alongside Notary.Validity. Use positional records and Result. Draft profiles go in the design doc for approval before implementation.

## Dependencies

(none)
