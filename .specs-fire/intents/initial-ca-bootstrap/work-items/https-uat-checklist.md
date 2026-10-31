---
id: https-uat-checklist
title: HTTPS user acceptance checklist
intent: initial-ca-bootstrap
complexity: low
mode: autopilot
status: completed
depends_on: [nginx-compose-service, ca-tls-bootstrap-runbook]
created: 2026-10-24T00:00:00Z
---

# Work Item: HTTPS user acceptance checklist

## Description

Add a UAT checklist to the runbook for verifying the HTTPS deployment end to end.

## Acceptance Criteria

- [ ] Checks: certificate chain trusted in a browser, correct SANs, HTTP redirects to HTTPS, access via both the LAN name and the Tailscale name
- [ ] Auth0 sign-in completes and the Blazor page keeps its websocket connection (no reconnect loop)
- [ ] A CRL URL in an issued certificate resolves over HTTPS and downloads
- [ ] Certificate expiry date recorded with a reminder

## Technical Notes

Docs only.

## Dependencies

- nginx-compose-service
- ca-tls-bootstrap-runbook
