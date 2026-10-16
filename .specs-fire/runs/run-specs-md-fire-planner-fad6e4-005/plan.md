---
run: run-specs-md-fire-planner-fad6e4-005
work_item: key-encryption-docs
intent: encrypt-stored-private-keys
mode: autopilot
checkpoint: none
approved_at: n/a
---

# Implementation Plan: Document the key storage exposure and fix

## Approach

The repository has no configuration docs and no `SECURITY.md`; the README is two lines. Add one focused page, `docs/private-key-storage.md`, and a short pointer plus a `NOTARY_APP_KEY` summary in the README. Dates and versions come from git: the regression is commit `f9aadd0` (2026-06-22), first contained in tag `v1.1.37`; the latest tag is `v1.1.45`.

## Files to Create

| File | Purpose |
|------|---------|
| `docs/private-key-storage.md` | Security notice, fix, operator actions (rotate CA keys), `NOTARY_APP_KEY`, startup migration and its log lines |

## Files to Modify

| File | Changes |
|------|---------|
| `README.md` | Security notice and Configuration section linking to the new page |

## Tests

(none; documentation only. The suite is re-run to confirm nothing else changed.)

---
