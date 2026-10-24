---
id: nginx-compose-service
title: NGINX service in the compose stack
intent: initial-ca-bootstrap
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-24T00:00:00Z
run_id: run-github-issue-51-4dca63-001
completed_at: 2026-10-24T22:09:25.001Z
---

# Work Item: NGINX service in the compose stack

## Description

Add an nginx service to `deploy/docker-compose.yml` with its config in `deploy/nginx/`, terminating TLS in front of `notary-web`.

## Acceptance Criteria

- [ ] `nginx` service publishes 80 and 443; certificate and key are bind-mounted read-only from a certs directory on the Pi (`/opt/notary/certs`)
- [ ] Port 80 redirects to 443
- [ ] Proxies to `notary-web:8080` with `Host`, `X-Forwarded-For` and `X-Forwarded-Proto`, plus websocket upgrade headers and long read timeouts for the Blazor Server connection
- [ ] `notary-web` no longer publishes 8080 on the host (reachable only through the compose network)
- [ ] TLS settings: TLS 1.2+ only, no weak ciphers; `server_tokens off`
- [ ] `docker compose config` validates and `nginx -t` passes against the config with sample certificates

## Technical Notes

The app already honours `X-Forwarded-For`/`X-Forwarded-Proto` (`Program.cs`), so the Auth0 redirect URI is built with https. `UseHttpsRedirection` runs before forwarded headers and cannot find an HTTPS port in the container, so it only logs a warning; NGINX does the redirect.

## Dependencies

(none)
