---
id: ca-purpose-profiles
title: Create certificate authorities by purpose
status: completed
created: 2026-10-13T00:00:00Z
completed_at: 2026-10-13T03:28:31.332Z
---

# Intent: Create certificate authorities by purpose

## Goal

Replace the one-size-fits-all Create CA form with a purpose selector (Root, TLS/SSL, User login, S/MIME, IoT devices, 802.1x/RADIUS, Custom marked discouraged). Each purpose applies a best-practice profile of key usage, extended key usage and constraints so the user does not set them by hand, and the purpose is carried on the CA certificate.

## Users

Anyone who creates CAs and issues certificates in Notary for the Raspberry Pi server farm.

## Problem

Creating a CA requires knowing how to set key usages and extended key usages correctly. That is error-prone and wrong values are a security risk. Best practice is one CA per purpose.

## Success Criteria

- The Create CA page presents the seven purposes, with Custom visibly marked as discouraged.
- A purpose can apply to a self-signed CA or an intermediate; Root is disabled when a parent is chosen.
- Each purpose maps to a fixed profile of key usage, extended key usage and basic constraints (profiles drafted for user approval).
- The purpose's extended key usages are written into the CA certificate so relying parties can enforce them.
- A child CA under a purpose CA must stay within its parent's allowed usages; the UI limits the choices and the server rejects anything else.
- On Create Certificate for a CA-issued certificate, the usage pickers show the issuing CA's profile, preselected and read-only; the server rejects any request outside the profile.
- Advanced usage editing is available only for self-signed certificates.
- Existing CAs have no purpose and are treated as Custom: they keep working unrestricted and need no data migration.

## Constraints

- Blazor with MudBlazor; `Result` pattern for validation failures; positional records for immutable profile definitions.
- The purpose is stored on the CA record, so the CA entity and its persistence model change.
- Must not break existing CAs, certificates or trust chains.

## Notes

Source: https://github.com/TheFlyingArcher/notary/issues/36

Decisions from capture: purposes apply to both self-signed and intermediate CAs; leaf usages are locked to the issuing CA's profile; existing CAs are treated as Custom; the CA certificate carries EKU restrictions.

Open items for decomposition: exact profile per purpose (key usage, EKU, path length); Custom behavior under a purpose parent (expected: limited to the parent's usages); whether existing CAs display a "Legacy/Custom" label.
