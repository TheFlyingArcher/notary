---
id: ca-tls-bootstrap-runbook
title: CA and TLS certificate bootstrap runbook
intent: initial-ca-bootstrap
complexity: medium
mode: confirm
status: completed
depends_on: [nginx-compose-service]
created: 2026-10-24T00:00:00Z
---

# Work Item: CA and TLS certificate bootstrap runbook

## Description

Write `docs/initial-ca-bootstrap.md`: the step-by-step procedure for creating the CA chain and the NGINX server certificate on the dev machine.

## Acceptance Criteria

- [ ] Starts from a clean dev database and the production `NOTARY_APP_KEY` value, with the reason (stored keys are encrypted with it)
- [ ] Create the root and intermediate CAs, then issue the server certificate with SANs `notary.unionsquared.lan`, `usqapp01.tailf1d37d.ts.net` and optional IPs
- [ ] Download the leaf and chain as PEM with the private key using the existing download dialog; confirm that this works, or record the gap
- [ ] Install the root certificate on Windows, macOS, iOS/iPadOS and Linux
- [ ] Add the DNS record for `notary.unionsquared.lan` in BIND9 and verify with `nslookup`/`ping`
- [ ] Auth0: set Allowed Callback URLs to `https://notary.unionsquared.lan/callback` and Allowed Logout URLs to `https://notary.unionsquared.lan/`
- [ ] Copy the certificate files to `/opt/notary/certs` with correct ownership and permissions
- [ ] Manual renewal procedure for the leaf, and the expiry date to track

## Technical Notes

Pi has no CA when first started; the data move is in `ca-data-migration`. Options chosen with the owner: dev-machine CA first.

## Dependencies

- nginx-compose-service
