---
id: run-notary-004
scope: wide
work_items:
  - id: config-value-source
    intent: separate-auth0-tenants
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
  - id: openid-tenant-guard
    intent: separate-auth0-tenants
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
  - id: auth0-client-secret
    intent: separate-auth0-tenants
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
  - id: auth0-tenant-docs-uat
    intent: separate-auth0-tenants
    mode: autopilot
    status: completed
    current_phase: review
    checkpoint_state: none
    current_checkpoint: null
current_item: null
status: completed
started: 2026-11-02T02:22:39.305Z
completed: 2026-11-02T02:30:35.211Z
---

# Run: run-notary-004

## Scope
wide (4 work items)

## Work Items
1. **config-value-source** (confirm) — completed
2. **openid-tenant-guard** (confirm) — completed
3. **auth0-client-secret** (confirm) — completed
4. **auth0-tenant-docs-uat** (autopilot) — completed


## Current Item
(all completed)

## Files Created
- `src/library/core/Configuration/NotaryConfigurationBinder.cs`: Binds NOTARY_* settings from any lookup, including user secrets
- `src/library/core/Configuration/OpenIdTenantGuard.cs`: Startup guard matching the Auth0 tenant label to the environment
- `src/test/NotaryConfigurationBinderTest.cs`: Tests for the configuration binder
- `src/test/OpenIdTenantGuardTest.cs`: Tests for the tenant guard

## Files Modified
- `src/web/Program.cs`: Use the binder, call the tenant guard, pass ClientSecret to the Auth0 SDK, drop the reflection loop
- `src/library/core/Configuration/NotaryOpenIdConfiguration.cs`: Add Tenant and Validate()
- `src/library/core/Configuration/NotaryConfiguration.cs`: Call OpenId.Validate()
- `src/test/NotaryConfigurationTest.cs`: OpenID client ID and secret validation tests
- `deploy/docker-compose.yml`: Require NOTARY_OPENID_TENANT
- `deploy/.env.example`: Document NOTARY_OPENID_TENANT=production
- `docs/ci-cd.md`: Auth0 tenants section, env table, UAT checklist
- `docs/initial-ca-bootstrap.md`: Production tenant wording for the Auth0 step

## Decisions
(none)


## Summary

- Work items completed: 4
- Files created: 4
- Files modified: 8
- Tests added: 37
- Coverage: 76%
- Completed: 2026-11-02T02:30:35.211Z
