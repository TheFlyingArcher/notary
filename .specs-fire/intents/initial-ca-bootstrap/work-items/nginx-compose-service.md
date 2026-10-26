---
id: nginx-compose-service
title: NGINX site for the Pi host NGINX
intent: initial-ca-bootstrap
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-24T00:00:00Z
run_id: run-github-issue-51-4dca63-001
completed_at: 2026-10-24T22:09:25.001Z
---

# Work Item: NGINX site for the Pi host NGINX

## Description

Provide a site config for the NGINX already installed on the Pi (shared with other projects), in `deploy/nginx/notary.conf`, terminating TLS in front of `notary-web`. Revised from an nginx compose service: the host NGINX already owns ports 80/443 and is the shared front door for future projects.

## Acceptance Criteria

- [ ] `deploy/nginx/notary.conf` is a host site (`sites-available`) for `notary.unionsquared.lan`; certificate and key are read from `/etc/nginx/certs/notary/`
- [ ] Port 80 redirects to 443
- [ ] Proxies to `127.0.0.1:8080` with `Host`, `X-Forwarded-For` and `X-Forwarded-Proto`, plus websocket upgrade headers and long read timeouts for the Blazor Server connection
- [ ] `notary-web` stays published on `127.0.0.1:8080` only; the upgrade map has a Notary-specific name so it cannot clash with other sites
- [ ] TLS settings: TLS 1.2+ only, no weak ciphers; no HSTS
- [ ] `docker compose config` validates and `nginx -t` passes against the config with a sample certificate in a Debian-style `sites-enabled` include

## Technical Notes

The app already honours `X-Forwarded-For`/`X-Forwarded-Proto` (`Program.cs`), so the Auth0 redirect URI is built with https. `UseHttpsRedirection` runs before forwarded headers and cannot find an HTTPS port in the container, so it only logs a warning; NGINX does the redirect.

## Dependencies

(none)
