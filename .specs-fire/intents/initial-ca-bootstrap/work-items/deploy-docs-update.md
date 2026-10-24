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

Update `docs/ci-cd.md` and `deploy/.env.example` for the host NGINX site and certificate directory.

## Acceptance Criteria

- [ ] `docs/ci-cd.md` Pi-side section describes installing `deploy/nginx/notary.conf` into the host NGINX, `/etc/nginx/certs/notary/`, and links to the bootstrap runbook
- [ ] `.env.example` documents `NOTARY_CRL_ENDPOINT=https://notary.unionsquared.lan`
- [ ] The instruction to copy `docker-compose.yml` to `/opt/notary` and the site config to `/etc/nginx/sites-available/` after changes is stated
- [ ] Deploy workflow note: the workflow only changes the image tag, so compose and nginx files must be copied by hand

## Technical Notes

Docs only.

## Dependencies

- nginx-compose-service
