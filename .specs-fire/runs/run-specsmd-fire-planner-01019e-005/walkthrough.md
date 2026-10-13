---
run: run-specsmd-fire-planner-01019e-005
work_item: certificates-page-create-buttons
intent: organize-certificates-by-ca
generated: 2026-10-14T01:05:00Z
mode: autopilot
---

# Implementation Walkthrough: Create buttons on the Certificates page

## Summary

The Certificates page now has two buttons: **Create Certificate** (for the CA selected in the dropdown) and **Create Self-Signed Certificate**. This completes the intent: select a CA, see its certificates, create certificates for it, or create a self-signed one separately.

## End-to-end UAT

1. Certificates page, two or more leaf-issuing CAs: the first CA by name is selected, with only its certificates listed.
2. Switch CA and Self-signed; the table changes; reload keeps the choice.
3. Pick a CA with no certificates: message instead of empty table.
4. Create Certificate: opens the form for that CA with its subject, key and usages applied and no CA dropdown; submit, and the new certificate appears under that CA.
5. Create Self-Signed Certificate: submit the form; it appears under Self-signed.
6. Self-signed selected: Create Certificate is disabled.
7. Sign in as a read-only user: both buttons disabled.
8. Try /certificates/create with no or a bad ca: redirected with a message.
