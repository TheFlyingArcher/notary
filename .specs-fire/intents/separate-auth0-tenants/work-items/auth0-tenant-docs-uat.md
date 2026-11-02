---
id: auth0-tenant-docs-uat
title: Document both Auth0 tenants and the UAT checklist
intent: separate-auth0-tenants
complexity: low
mode: autopilot
status: completed
depends_on:
  - config-value-source
  - openid-tenant-guard
  - auth0-client-secret
created: 2026-11-02T01:59:37Z
run_id: run-notary-004
completed_at: 2026-11-02T02:30:35.211Z
---

# Work Item: Document both Auth0 tenants and the UAT checklist

## Description

Explain how to run Notary against a dev tenant locally and a production tenant on the Pi, and how to verify each.

## Acceptance Criteria

- [ ] `docs/ci-cd.md` and `docs/initial-ca-bootstrap.md` section 4: production `.env` values including `NOTARY_OPENID_TENANT=production`, flagged first as a required step before deploying this release
- [ ] A local-development section: `dotnet user-secrets set` commands for `NOTARY_OPENID_DOMAIN`, `NOTARY_OPENID_CLIENTID`, `NOTARY_OPENID_CLIENTSECRET`, `NOTARY_OPENID_TENANT` on `src/web`
- [ ] Callback and logout URLs listed per tenant (dev: `https://localhost:17244/callback` and `/`; production: `https://notary.unionsquared.lan/callback` and `/`), and a note that localhost URLs belong only on the dev tenant
- [ ] A UAT checklist covering: dev sign-in works, a production label under Development fails with the guard message, a missing label fails, and Pi sign-in works after deploy
- [ ] `src/web/CHANGELOG.md` gets an entry
- [ ] The FIRE state check passes

## Technical Notes

Never put a real domain, client ID or secret in the docs; use placeholders. The Application Login URI stays blank (the `.lan` name was rejected by Auth0).

## Dependencies

- config-value-source
- openid-tenant-guard
- auth0-client-secret
