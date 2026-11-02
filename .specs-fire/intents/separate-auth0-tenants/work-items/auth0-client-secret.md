---
id: auth0-client-secret
title: Pass the Auth0 client secret to the SDK
intent: separate-auth0-tenants
complexity: medium
mode: confirm
status: completed
depends_on:
  - openid-tenant-guard
created: 2026-11-02T01:59:37Z
run_id: run-notary-004
completed_at: 2026-11-02T02:29:44.506Z
---

# Work Item: Pass the Auth0 client secret to the SDK

## Description

`AddAuth0WebAppAuthentication` in `src/web/Program.cs` sets only `Domain` and `ClientId`; `NOTARY_OPENID_CLIENTSECRET` is read and never used. Pass the secret so sign-in uses the authorization code flow.

## Acceptance Criteria

- [ ] `o.ClientSecret = config.OpenId.ClientSecret` is set in the Auth0 options
- [ ] Startup validation requires a non-empty client secret whenever an Auth0 domain is set, with a message that never prints the value
- [ ] Unit tests cover the secret-required rule
- [ ] UAT: sign-in and sign-out against the dev tenant succeed locally from Visual Studio
- [ ] UAT: the docs list what to check on the Pi after deploy (sign-in, sign-out, `docker compose logs notary-web` shows no OIDC errors)

## Technical Notes

Auth0's SDK docs mention the client secret for obtaining API access tokens; the sign-in flow it selects is not documented there, so UAT is the proof. Auth0 application type must stay Regular Web Application.

## Dependencies

- openid-tenant-guard
