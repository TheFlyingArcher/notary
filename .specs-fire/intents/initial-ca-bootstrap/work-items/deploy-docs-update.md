---
id: deploy-docs-update
title: Update deploy docs and env template for NGINX and HTTPS
intent: initial-ca-bootstrap
complexity: low
mode: autopilot
status: pending
depends_on: [nginx-compose-service]
created: 2026-10-24T00:00:00Z
---

# Work Item: Update deploy docs and env template for NGINX and HTTPS

## Description

Update `docs/ci-cd.md` and `deploy/.env.example` for the NGINX service and certificate directory.

## Acceptance Criteria

- [ ] `docs/ci-cd.md` Pi-side section describes the nginx service, `/opt/notary/certs`, and links to the bootstrap runbook
- [ ] `.env.example` documents `NOTARY_CRL_ENDPOINT=https://notary.unionsquared.lan`
- [ ] The instruction to copy `docker-compose.yml` and the nginx config to `/opt/notary` after changes is stated
- [ ] Deploy workflow note: the workflow only changes the image tag, so compose and nginx files must be copied by hand

## Technical Notes

Docs only.

## Dependencies

- nginx-compose-service
