---
id: key-strength-model
title: Key strength model
intent: mixed-chain-key-strength
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-26T00:00:00Z
run_id: run-specsmd-fire-planner-02371b-004
completed_at: 2026-10-27T00:10:37.601Z
---

# Work Item: Key strength model

## Description

Add a static class in `src/library/core` that maps a key (algorithm, curve, length) to NIST SP 800-57 security bits and checks a subject against its issuer.

## Acceptance Criteria

- [ ] EC: P-256 = 128, P-384 = 192, P-521 = 256
- [ ] RSA: below 2048 = 80, 2048-3071 = 112, 3072-7679 = 128, 7680-15359 = 192, 15360 and above = 256 (off-table sizes take the row below)
- [ ] A check returns `Result.Fail` when the subject's bits are lower than the issuer's, and success when equal or higher, across key types (RSA-3072 and P-256 may issue to each other)
- [ ] A missing curve for EC or missing length for RSA is a clear failure, not a guess
- [ ] `dotnet build` passes

## Technical Notes

Pure and stateless so the services, the web pages and the tests can all share it. Use `Result` rather than exceptions. The UI work items also need a way to ask 'is this option at least N bits' for filtering.

## Dependencies

(none)
