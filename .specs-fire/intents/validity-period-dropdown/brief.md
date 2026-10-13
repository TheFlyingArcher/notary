---
id: validity-period-dropdown
title: Certificate/CA validity period dropdown
status: completed
created: 2026-10-12T00:00:00Z
completed_at: 2026-10-13T01:21:48.601Z
---

# Intent: Certificate/CA validity period dropdown

## Goal

Replace the free-text "Valid For" / "CA Validity" fields in the Create CA and Create Certificate pages with dropdowns of industry-standard validity periods, and enforce that no certificate or child CA outlives its issuing CA.

## Users

Anyone who runs the notary UI to create CAs and leaf certificates for the Raspberry Pi server farm.

## Problem

Free text allows insecure or odd validity periods (e.g. 120 months). Users cannot see the issuing CA's validity, so a bad guess fails with an error. This is poor UI/UX.

## Success Criteria

- Both pages use a dropdown instead of a free-text validity field.
- Options are per type (root CA, intermediate CA, leaf), each with a marked recommended default; leaf lists include day-based options (e.g. 30, 90 days).
- Month options display as "[months] months ([years])", e.g. "48 months (4 years)"; day options display in days.
- Options exceeding the selected issuer's expiry are shown disabled with a reason (e.g. "exceeds issuer expiry"); changing the issuer re-evaluates the options.
- The service layer also rejects a child whose NotAfter is later than its issuer's, returning a Result rather than throwing.
- The existing 12-month minimum on leaf certificates is removed or replaced to fit the new lists.
- Existing certificates and CAs are unaffected.

## Constraints

- Blazor with MudBlazor, in the existing pages under src/web/Pages.
- Follow repo C# standards: Result pattern, positional records, Allman bracing, no exceptions for validation failures.
- Self-signed roots have no parent cap.
- Requests already carry NotBefore/NotAfter datetimes, so day-based leaf options need no model change.

## Notes

Source: https://github.com/TheFlyingArcher/notary/issues/35

Open items for decomposition: exact per-type period lists and recommended defaults (draft for user approval); server-side error shape for a child that outlives its issuer.
