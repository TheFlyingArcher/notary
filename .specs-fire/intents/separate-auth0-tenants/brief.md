---
id: separate-auth0-tenants
title: Separate Auth0 tenants for development and production
status: completed
created: 2026-11-02T01:57:14Z
completed_at: 2026-11-02T02:30:35.211Z
---

# Intent: Separate Auth0 tenants for development and production

## Goal

Notary uses one Auth0 tenant for local development and a different one for production on the Raspberry Pi, and cannot be started against the wrong one.

## Users

The maintainer, as developer and operator.

## Problem

The dev tenant also serves production. A change made while developing (a callback URL, a rule, a user, a deleted application) can break production sign-in.

## Success Criteria

- Local runs read the dev tenant (domain, client ID, client secret) from `dotnet user-secrets` on the web project
- The Pi reads the production tenant from `/opt/notary/.env`; the compose file and `.env.example` document it
- A startup guard stops Notary when the environment and the tenant do not match, with a clear message that never prints the secret
- `Program.cs` passes `ClientSecret` to the Auth0 SDK, so sign-in uses the authorization code flow (today only `Domain` and `ClientId` are set)
- Docs cover setting up both tenants, including the callback and logout URLs for each, and a UAT checklist
- Tests cover the guard; the build and all tests pass

## Constraints

- The existing `NOTARY_OPENID_*` variable names stay the same; one new variable, `NOTARY_OPENID_TENANT`, must be added to the Pi `.env` before the release that adds the guard is deployed
- No secrets in git or chat; the client IDs and secrets are supplied by the maintainer when ready and recorded only in user secrets and the Pi `.env`
- Stays runnable and debuggable from Visual Studio (UAT workflow)
- Out of scope: serving several tenants at once at runtime

## Notes

Guard design (settled at decomposition): each tenant is labelled `development` or `production` with `NOTARY_OPENID_TENANT`, and startup fails when the label does not match `ASPNETCORE_ENVIRONMENT`. Settings are also read from user secrets via `IConfiguration`, because `Program.cs` currently reads only environment variables.

Auth0 settings per tenant are in `docs/initial-ca-bootstrap.md` section 4 and `docs/ci-cd.md`. Production host: `https://notary.unionsquared.lan`; the Application Login URI was rejected for the `.lan` name and is left blank.
