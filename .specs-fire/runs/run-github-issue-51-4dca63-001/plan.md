---
run: run-github-issue-51-4dca63-001
work_item: nginx-compose-service
intent: initial-ca-bootstrap
mode: confirm
checkpoint: plan
approved_at: pending
---

# Implementation Plan: NGINX service in the compose stack

## Approach

Add an `nginx:alpine` service to `deploy/docker-compose.yml` that terminates TLS and proxies to `notary-web:8080` over the compose network. The config lives in `deploy/nginx/default.conf` and is bind-mounted read-only; the certificate files come from a directory on the Pi (`NOTARY_CERTS_DIR`, default `./certs`, i.e. `/opt/notary/certs`) mounted read-only at `/etc/nginx/certs`. `notary-web` stops publishing 8080 on the host so the only entry point is NGINX.

## Files to Create

| File | Purpose |
|------|---------|
| `deploy/nginx/default.conf` | HTTP to HTTPS redirect, TLS server block, proxy with forwarded headers and websocket upgrade |

## Files to Modify

| File | Changes |
|------|---------|
| `deploy/docker-compose.yml` | Add `nginx` service (ports 80/443, mounts, depends on `notary-web`); remove the `127.0.0.1:8080` port publish from `notary-web` and use `expose` |
| `deploy/.env.example` | Document `NOTARY_CERTS_DIR` |

## Technical Details

- **Certificates:** `fullchain.pem` (leaf then intermediate) and `privkey.pem` in the certs directory. Fixed names keep the config and the runbook simple. NGINX will not start until they exist, which is expected before the CA bootstrap.
- **TLS:** TLS 1.2 and 1.3 only, server cipher preference off (modern defaults), `server_tokens off`, session cache on. No HSTS: it would lock browsers out of the site if the private CA certificate expires or is replaced, and this is an internal host.
- **Redirect:** port 80 returns 301 to `https://$host$request_uri`.
- **Proxy headers:** `Host $host`, `X-Forwarded-For $remote_addr` (set, not appended, because the app trusts forwarded headers from any source and NGINX is the edge), `X-Forwarded-Proto $scheme`.
- **Blazor Server:** `map $http_upgrade $connection_upgrade` plus `proxy_http_version 1.1`, `Upgrade` and `Connection` headers, `proxy_read_timeout`/`proxy_send_timeout` of 1 hour, `proxy_buffering off`.
- **Upload size:** `client_max_body_size 10m`.
- **App behaviour (unchanged):** `UseHttpsRedirection` runs before forwarded headers and finds no HTTPS port in the container, so it only logs a warning. NGINX does the redirect.

## Verification (no C# tests: configuration only)

- `docker compose config` validates with dummy variables.
- `nginx -t` runs inside the `nginx:alpine` image against the config with a throwaway self-signed certificate.
- Optional smoke test: run nginx with the config and curl `http://localhost` (expect 301) and `https://localhost` with `-k` (expect a proxy error 502 without the app, proving TLS and routing).

---
*Plan awaiting approval.*
