---
id: hierarchical-ca-ordering
title: Hierarchical ordering of CAs
status: completed
created: 2026-10-14T00:00:00Z
source: https://github.com/TheFlyingArcher/notary/issues/40
completed_at: 2026-10-14T23:32:46.918Z
---

# Intent: Hierarchical ordering of CAs

## Goal

Show certificate authorities on the CA page as a hierarchy, with each intermediate CA directly under its issuer, instead of in creation order.

## Users

Notary operators managing CA chains for the Raspberry Pi server farm.

## Problem

The `/ca` table lists CAs in creation order, so it is unclear which CA belongs to which. Naming is not a reliable way to convey order.

## Success Criteria

- Table orders CAs depth-first with indentation showing depth, in a single MudTable (no tree control); multi-level chains supported
- "Create Certificate Authority" becomes "Create Root CA"; root creation has no purpose dropdown
- Every CA row that can issue CAs has a round "+" button to add an intermediate CA; the purpose dropdown there has no Root option
- Intermediate rows show a purpose icon: TLS/SSL http, User login assignment_ind, S/MIME email, IoT devices devices, 802.1x/RADIUS wifi_password, Custom tune
- Every row has Details and Download certificate buttons (existing ones are reused)
- Existing API and UI tests pass; new tests cover ordering and the creation modes

## Constraints

- Blazor/MudBlazor UI in src/web; reuse CA purpose profiles and server-side enforcement
- Leaf certificates stay on the Certificates page (out of scope)
- Out of scope: pagination, search, tree control

## Notes

Source: GitHub issue #40 (label: enhancement). `/ca` already has a Details button (certificate?slug=) and a Download dialog on every row; they are kept rather than rebuilt.
