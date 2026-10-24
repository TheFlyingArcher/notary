---
id: initial-ca-bootstrap
title: Initial CA and HTTPS bootstrap for the Raspberry Pi
status: in_progress
created: 2026-10-24T00:00:00Z
---

# Intent: Initial CA and HTTPS bootstrap for the Raspberry Pi

GitHub issue: TheFlyingArcher/notary#50

## Goal

Put Notary behind NGINX with a trusted HTTPS certificate on the Raspberry Pi, without the catch-22 that the CA issuing that certificate is created by the application that needs HTTPS first. Approach chosen: create the CA chain on the dev machine first, trust it, issue the NGINX certificate from it, and move the CA data into the production MongoDB Atlas database (issue option 2).

## Users

The owner, reaching Notary over Tailscale and from other devices on the home LAN. Auth0 redirects the browser back to Notary, so it needs an HTTPS callback URL.

## Problem

Production cannot serve TLS until a certificate exists, and the CA that issues it is created inside Notary. Starting over HTTP (issue option 1) would send the first Auth0 sign-in over plain HTTP.

## Success Criteria

- `https://notary.unionsquared.lan` loads from the Pi with a certificate issued by the owner's Notary CA; browsers on the owner's devices trust it once the root CA is installed.
- The leaf certificate covers every name in use: `notary.unionsquared.lan`, the Tailscale name `usqapp01.tailf1d37d.ts.net`, and optionally the IP addresses.
- Auth0 callback and logout URLs are set for the HTTPS hostname, and sign-in works end to end through NGINX (including the Blazor Server websocket).
- CA data moved to production still works: production uses the same `NOTARY_APP_KEY` the dev database was created with, and the startup log shows `Private key migration: 0 migrated, ... 0 failed.`
- Dev testing CAs are not carried into production (clean dev database before creating the real CA).
- Issued certificates carry CRL distribution points on the HTTPS hostname (`NOTARY_CRL_ENDPOINT`).

## Constraints

- Production is a Raspberry Pi 4 deployed by the GitHub Actions Deploy workflow (#51), with MongoDB Atlas as the database (MongoDB 5+ cannot run on a Pi 4; see #59).
- The app listens on port 8080 in its container and is published only on `127.0.0.1:8080`.
- Private keys are encrypted with `NOTARY_APP_KEY`; changing it makes stored keys unreadable.
- Name resolution: the owner runs a BIND9 DNS server (`ns1.unionsquared.lan`, 192.168.1.11) that resolves all `*.unionsquared.lan` names. Verify it from a machine on the same network with `ping ns1.unionsquared.lan`. `.lan` is not an officially reserved name (`home.arpa` is), which is acceptable for a private CA.
- Tailscale clients do not use the LAN names unless their DNS reaches BIND9, so the Tailscale name must be in the certificate's SANs.
- The app's existing downloads (PEM, PFX, CER) are expected to cover exporting the leaf certificate and key; confirm during implementation.

## Out of Scope

- Application code changes such as a CA import/export tool.
- Automated certificate renewal (renew by hand; the runbook documents it).

## Notes

Deliverables: an NGINX service and config in `deploy/docker-compose.yml`, and a runbook in `docs/`. Open point for the plan: Blazor Server needs websocket upgrade headers in NGINX; forwarded headers are already honoured by the app.
