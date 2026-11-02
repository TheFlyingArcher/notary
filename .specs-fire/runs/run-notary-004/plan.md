---
run: run-notary-004
work_item: config-value-source
intent: separate-auth0-tenants
mode: confirm
checkpoint: plan
approved_at: pending
---

# Implementation Plan: Separate Auth0 tenants for development and production

One approval covers the three confirm items below; the docs item (autopilot) follows without a checkpoint.

## Work Item: config-value-source

### Approach

Replace the reflection loop in `src/web/Program.cs` (environment variables only) with a testable `NotaryConfigurationBinder` in the core library. It binds every `[NotaryEnvironmentVariable]` property from a `Func<string, string?>` lookup. `Program.cs` passes `key => builder.Configuration[key]`, which covers user secrets and environment variables, with environment variables winning. Empty values leave defaults untouched, as today. `src/setup` keeps its own copy (out of scope).

### Files to Create

| File | Purpose |
|------|---------|
| `src/library/core/Configuration/NotaryConfigurationBinder.cs` | Binds the root, Database and OpenId settings from a lookup |
| `src/test/NotaryConfigurationBinderTest.cs` | Binding, override, empty-value and default cases |

### Files to Modify

| File | Changes |
|------|---------|
| `src/web/Program.cs` | Use the binder; delete `SetEnvironmentVariables` and `SetPropertiesEnvVariable` |

## Work Item: openid-tenant-guard

### Approach

Add `Tenant` (`NOTARY_OPENID_TENANT`) to `NotaryOpenIdConfiguration` and a small `OpenIdTenantGuard` class. When a Domain is configured the label must be `development` or `production`. `development` is allowed only when the environment is `Development`; `production` is allowed in any other environment. Anything else throws `InvalidOperationException` naming the setting, the label and the environment, never the secret. `Program.cs` calls it right after `config.Validate()` with `builder.Environment.EnvironmentName`. With no Domain, nothing changes.

### Files to Create

| File | Purpose |
|------|---------|
| `src/library/core/Configuration/OpenIdTenantGuard.cs` | Matches the tenant label to the environment |
| `src/test/OpenIdTenantGuardTest.cs` | Pairings, missing and unknown label, no-domain case, secret never in the message |

### Files to Modify

| File | Changes |
|------|---------|
| `src/library/core/Configuration/NotaryOpenIdConfiguration.cs` | Add `Tenant` |
| `src/web/Program.cs` | Call the guard at startup |
| `deploy/docker-compose.yml` | Pass `NOTARY_OPENID_TENANT` (required) |
| `deploy/.env.example` | Document `NOTARY_OPENID_TENANT=production` |

## Work Item: auth0-client-secret

### Approach

Set `o.ClientSecret` from `config.OpenId.ClientSecret` in `AddAuth0WebAppAuthentication`. Add `NotaryOpenIdConfiguration.Validate()`, called from `NotaryConfiguration.Validate()`: when a Domain is set, ClientId and ClientSecret are required, and the message names the variable without printing a value.

### Files to Modify

| File | Changes |
|------|---------|
| `src/web/Program.cs` | Pass `ClientSecret` to the SDK |
| `src/library/core/Configuration/NotaryOpenIdConfiguration.cs` | Add `Validate()` |
| `src/library/core/Configuration/NotaryConfiguration.cs` | Call `OpenId.Validate()` |
| `src/test/NotaryConfigurationTest.cs` | Secret and client ID required when a Domain is set |

## Work Item: auth0-tenant-docs-uat

### Approach

Docs only: the production `.env` steps with `NOTARY_OPENID_TENANT=production` called out first, a local-development section with `dotnet user-secrets` commands for `src/web`, the callback and logout URLs per tenant, a UAT checklist, and a change log entry.

### Files to Modify

| File | Changes |
|------|---------|
| `docs/ci-cd.md` | Env table row for `NOTARY_OPENID_TENANT`, tenant separation and UAT |
| `docs/initial-ca-bootstrap.md` | Section 4: production tenant label and URLs |
| `src/web/CHANGELOG.md` | Entry for the change |

## Technical Details

- Tests run with `dotnet test ./src/test/`; NUnit 4, Allman braces, names describe behavior.
- Branch: `feature/separate-auth0-tenants`.
- Merging the guard means the Pi needs `NOTARY_OPENID_TENANT=production` in `/opt/notary/.env` before deploying.

---
*Plan approved at checkpoint. Execution follows.*
