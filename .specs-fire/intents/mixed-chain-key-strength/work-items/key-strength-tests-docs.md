---
id: key-strength-tests-docs
title: Key strength tests and docs
intent: mixed-chain-key-strength
complexity: medium
mode: confirm
status: completed
depends_on:
  - child-ca-own-key-settings
  - leaf-key-strength-floor
created: 2026-10-26T00:00:00Z
run_id: run-specsmd-fire-planner-02371b-010
completed_at: 2026-10-27T01:08:47.764Z
---

# Work Item: Key strength tests and docs

## Description

Cover the strength rule end to end and document it.

## Acceptance Criteria

- [ ] Tests for CA and leaf issuance across RSA→RSA, RSA→EC, EC→RSA and EC→EC
- [ ] Boundary cases at equal strength (for example RSA-3072 under P-256 and the reverse) and rejection one step below
- [ ] An existing chain that already violates the rule still loads and validates
- [ ] Docs describe mixed chains, the strength table and the rule
- [ ] `dotnet test ./src/test/` passes

## Technical Notes

Extend the existing fixtures and `FakeKeyService`, which now honors the requested algorithm, curve and size. Keep RSA tests to small sizes where the strength rule allows, to keep test time down. Docs live in `docs/` and the README.

## Dependencies

- child-ca-own-key-settings
- leaf-key-strength-floor
