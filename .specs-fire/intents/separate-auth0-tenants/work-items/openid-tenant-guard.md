---
id: openid-tenant-guard
title: Startup guard that matches the Auth0 tenant to the environment
intent: separate-auth0-tenants
complexity: medium
mode: confirm
status: completed
depends_on:
  - config-value-source
created: 2026-11-02T01:59:37Z
run_id: run-notary-004
completed_at: 2026-11-02T02:27:32.585Z
---

# Work Item: Startup guard that matches the Auth0 tenant to the environment

## Description

Add `NOTARY_OPENID_TENANT` (`development` or `production`) to `NotaryOpenIdConfiguration`, set beside the domain in user secrets and the Pi `.env`. A validator fails startup when the label is missing or does not match `ASPNETCORE_ENVIRONMENT`, so a dev run cannot use the production tenant and the Pi cannot use the dev tenant.

## Acceptance Criteria

- [ ] `Development` environment with `production` label fails; `Production` with `development` fails
- [ ] `Development` with `development` and `Production` with `production` pass
- [ ] A missing or unknown label fails whenever a domain is configured; with no OpenId settings at all, behavior is unchanged
- [ ] The error names the setting, the environment and the label, and never includes the client secret
- [ ] The guard runs from `Program.cs` at startup, next to `config.Validate()`
- [ ] `deploy/docker-compose.yml` passes `NOTARY_OPENID_TENANT` (required, like the other OpenID values) and `deploy/.env.example` documents it
- [ ] Unit tests cover the four pairings, a missing label, an unknown label and the secret-not-logged rule

## Technical Notes

Keep the check in its own small class (single responsibility), not inside `NotaryConfiguration`. Environment names compare case-insensitively. `deploy/docker-compose.yml` already sets `ASPNETCORE_ENVIRONMENT: Production`. After this merges and deploys, the Pi will not start until `/opt/notary/.env` has `NOTARY_OPENID_TENANT=production`; the docs item calls this out first.

## Dependencies

- config-value-source
