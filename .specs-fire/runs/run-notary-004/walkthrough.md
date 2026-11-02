---
run: run-notary-004
work_item: config-value-source, openid-tenant-guard, auth0-client-secret, auth0-tenant-docs-uat
intent: separate-auth0-tenants
generated: 2026-11-02T02:31:00Z
mode: confirm (3 items), autopilot (1 item)
---

# Implementation Walkthrough: Separate Auth0 tenants for development and production

## Summary

Notary can now run its local development against one Auth0 tenant and production against another. Settings are read from user secrets as well as environment variables, each tenant carries a `development` or `production` label, and a startup guard refuses a label that does not suit the environment. The Auth0 client secret is now passed to the SDK.

## Structure Overview

Startup in `Program.cs` now runs in this order: bind settings from `builder.Configuration` (environment variables and user secrets), run the existing configuration validation (which now also checks the OpenID client credentials), run the tenant guard against the hosting environment, then register Auth0 with domain, client ID and client secret. The binder, the guard and the OpenID validation are separate small units in the core library, each testable without a web host.

## Files Changed

### Created

| File | Purpose |
|------|---------|
| `src/library/core/Configuration/NotaryConfigurationBinder.cs` | Fills every `[NotaryEnvironmentVariable]` property from a lookup function |
| `src/library/core/Configuration/OpenIdTenantGuard.cs` | Matches the tenant label to the environment |
| `src/test/NotaryConfigurationBinderTest.cs` | Binder tests |
| `src/test/OpenIdTenantGuardTest.cs` | Guard tests |

### Modified

| File | Changes |
|------|---------|
| `src/web/Program.cs` | Uses the binder and the guard, passes `ClientSecret`, removes the reflection loop and an unused using |
| `src/library/core/Configuration/NotaryOpenIdConfiguration.cs` | Adds `Tenant` (`NOTARY_OPENID_TENANT`) and `Validate()` |
| `src/library/core/Configuration/NotaryConfiguration.cs` | Calls `OpenId?.Validate()` |
| `src/test/NotaryConfigurationTest.cs` | Client ID and secret validation tests |
| `deploy/docker-compose.yml` | Requires `NOTARY_OPENID_TENANT` |
| `deploy/.env.example` | Documents `NOTARY_OPENID_TENANT=production` |
| `docs/ci-cd.md` | Auth0 tenants section, env table row, UAT checklist |
| `docs/initial-ca-bootstrap.md` | Production tenant wording for the Auth0 step |

## Key Implementation Details

### 1. Settings now come from configuration

The old loop read only process environment variables, so user secrets (which feed `IConfiguration`) were invisible. The binder takes a lookup, and `Program.cs` passes `builder.Configuration`, which includes both. Environment variables still win over user secrets.

### 2. The guard

When a domain is set, the label must be `development` or `production`. `development` is accepted only in the `Development` environment; `production` in any other environment. Matching is case-insensitive.

## Security Considerations

| Concern | Approach |
|---------|----------|
| Dev run hitting production sign-in | Guard stops startup when a `production` label meets `Development` |
| Production using the dev tenant | Guard stops startup when a `development` label meets any other environment |
| Secret exposure | No message includes the client secret; tests assert this. Secrets live in user secrets locally and in the Pi `.env`, never in git |
| Code flow | `ClientSecret` is now set on the Auth0 options |

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| How the guard knows the tenant | An explicit `NOTARY_OPENID_TENANT` label | Domains alone say nothing about purpose; an explicit label is hard to set by accident |
| Which environments accept `production` | Every environment except `Development` | A Staging environment can use the production tenant; dev machines never can |
| Label required or optional | Required whenever a domain is set | An optional label would let the old setup bypass the guard |

## Deviations from Plan

- No `src/web/CHANGELOG.md` entry. That file is filled automatically from published GitHub Release notes, so a manual entry would duplicate. Suggested release notes are below.
- The new tests total 37 cases (6 binder, 21 guard, 10 configuration) rather than the counts first drafted.

## Dependencies Added

None.

## How to Verify

1. **Unit tests**

   ```bash
   dotnet test ./src/test/
   ```

   Expected: 714 passed, 0 failed.

2. **Local sign-in.** Set the four user secrets from the Auth0 tenants section of `docs/ci-cd.md` and run the `https` profile in Visual Studio. Expected: sign-in and sign-out work against the development tenant.

3. **Guard.** Follow the UAT checklist in `docs/ci-cd.md` (wrong label, missing label, missing secret). Expected: each stops startup with a message naming the setting.

4. **Pi.** Add `NOTARY_OPENID_TENANT=production` to `/opt/notary/.env` before deploying. Expected: `docker compose config` is clean and sign-in works at `https://notary.unionsquared.lan`.

## Test Coverage

- Tests added: 37
- Coverage: 76.1% overall; the new classes 92 to 100%
- Status: passing (714)

## Ready for Review

- [x] All acceptance criteria met (UAT items pending with the user)
- [x] Tests passing
- [x] No critical issues
- [x] Documentation updated
- [x] Developer notes captured

## Developer Notes

- Deploying this release without `NOTARY_OPENID_TENANT=production` in the Pi `.env` makes `docker compose up` fail with "Set NOTARY_OPENID_TENANT=production in .env". Add the line first.
- A dev setup that has a domain and client ID but no secret now fails at startup.
- Remove old `NOTARY_OPENID_*` environment variables from your PC, or they shadow the user secrets.
- Suggested release notes: "Notary can use separate Auth0 tenants for development and production. A startup guard refuses a tenant that does not match the environment, local settings can live in user secrets, and the Auth0 client secret is now used for sign-in. Add NOTARY_OPENID_TENANT=production to the Pi .env before deploying."

---
*Generated by specs.md - fabriqa.ai FIRE Flow Run run-notary-004*
