# Test Report: run-notary-004

## Work Item: config-value-source

### Test Results
- Passed: 6 new test cases in `NotaryConfigurationBinderTest` (31 in the configuration filter); full suite 683 passed, 0 failed, 0 skipped

### Acceptance Criteria Validation
- [x] A `NOTARY_*` value reaches `config`, `config.Database` and `config.OpenId` (`BindSetsRootDatabaseAndOpenIdSettings`)
- [x] Precedence of environment variables over user secrets is the default `IConfiguration` order; the binder reads whatever `builder.Configuration` returns (checked in UAT)
- [x] Missing or empty values keep defaults (`BindKeepsTheCurrentValueWhenTheSettingIsMissingOrEmpty`, `BindLeavesDefaultsWhenNothingIsSet`)
- [x] `Program.cs` uses the binder and the reflection loop is removed; the web project builds
- [x] Same environment variables still work: they are part of `builder.Configuration`
- [x] Unit tests cover binding, defaults and null arguments

---

## Work Item: openid-tenant-guard

### Test Results
- Passed: 21 new test cases in `OpenIdTenantGuardTest`; the configuration filter ran 52 passed, 0 failed

### Acceptance Criteria Validation
- [x] `production` label in Development fails; `development` label in Production or Staging fails (`ValidateRejects...`)
- [x] `development` in Development and `production` in Production or Staging pass (`ValidateAcceptsATenantThatSuitsTheEnvironment`)
- [x] Missing or unknown label fails when a domain is set; no domain means no check (`ValidateRejectsAMissingOrUnknownTenant...`, `ValidateDoesNothingWhenNoDomainIsConfigured`)
- [x] The message names the setting, label and environment and never the client secret (`ValidateNeverPutsTheClientSecretInTheMessage`)
- [x] `Program.cs` calls the guard right after `config.Validate()`
- [x] `docker-compose.yml` requires `NOTARY_OPENID_TENANT`; `.env.example` documents it

---

## Work Item: auth0-client-secret

### Test Results
- Passed: 10 new test cases in `NotaryConfigurationTest`. Full suite: 714 passed, 0 failed, 0 skipped. Overall line coverage 76.1%; `OpenIdTenantGuard` and `NotaryOpenIdConfiguration` 100%, `NotaryConfigurationBinder` 92%.

### Acceptance Criteria Validation
- [x] `o.ClientSecret = config.OpenId.ClientSecret` is set in the Auth0 options (web project builds)
- [x] A domain without a client ID or without a client secret fails startup validation, naming the variable and never the value (`ValidateRequiresAClientId...`, `ValidateRequiresAClientSecret...`)
- [x] No domain, or a complete configuration, passes (`ValidateIgnoresOpenIdSettingsWhenNoDomainIsSet`, `ValidateAcceptsACompleteOpenIdConfiguration`)
- [ ] UAT (user): sign-in and sign-out against the dev tenant from Visual Studio; Pi sign-in after deploy. Covered by the checklist in the docs item.

---

## Work Item: auth0-tenant-docs-uat

### Test Results
- Documentation only; no new tests. The full suite still passes (714 passed, 0 failed). The FIRE state check passes.

### Acceptance Criteria Validation
- [x] `docs/ci-cd.md`: env table, Auth0 tenants section, and `NOTARY_OPENID_TENANT=production` flagged as a step to take before deploying
- [x] `docs/initial-ca-bootstrap.md` section 4 points at the production tenant, drops the advice to keep dev URLs, and links to the new section
- [x] `dotnet user-secrets set` commands for the four dev values
- [x] Callback and logout URLs listed per tenant; localhost only on the dev tenant
- [x] UAT checklist covering dev sign-in, the three startup refusals, Pi pre-deploy and post-deploy checks, and tenant separation
- [ ] Deviation: no `src/web/CHANGELOG.md` entry. That file is filled automatically from published GitHub Release notes by `.github/workflows/changelog.yml`, so a hand-written entry would be duplicated. Suggested release notes are in the walkthrough.
