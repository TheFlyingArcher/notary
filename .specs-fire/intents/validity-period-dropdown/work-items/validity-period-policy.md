---
id: validity-period-policy
title: Shared validity period policy
intent: validity-period-dropdown
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-12T00:00:00Z
run_id: run-specsmd-fire-528fef-001
completed_at: 2026-10-13T00:06:38.978Z
---

# Work Item: Shared validity period policy

## Description

Add a UI-agnostic validity policy to the library: a `ValidityPeriod` record (amount + unit of months or days), per-type option lists (root CA, intermediate CA, leaf) with a recommended default, a display formatter, and a function that evaluates each option against an issuer's NotAfter.

## Acceptance Criteria

- [ ] Positional record `ValidityPeriod` supports months and days units and can compute NotAfter from a start date (AddMonths/AddDays)
- [ ] Per-type lists exist for root, intermediate and leaf, each with exactly one recommended option (draft: root 10/15/20y rec 20y; intermediate 3/5/10y rec 5y; leaf 30d/90d/6/12/13/24 months rec 12 months), approved by the user at the plan checkpoint
- [ ] Formatter outputs "48 months (4 years)" for month periods (years omitted or fractional-safe when not whole) and "90 days" for day periods
- [ ] Evaluation function marks an option as allowed or disallowed with a reason (e.g. "exceeds issuer expiry") given an optional issuer NotAfter; no issuer means all allowed
- [ ] Unit tests cover lists, formatting, cap logic, and the boundary where NotAfter equals the issuer's

## Technical Notes

Place in the library core so web and services both use it. Follow Result pattern and positional-record conventions. Evaluate against the same "now" used when issuing.

## Dependencies

(none)
