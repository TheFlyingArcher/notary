---
id: config-value-source
title: Read Notary settings from configuration so user secrets work
intent: separate-auth0-tenants
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-11-02T01:59:37Z
run_id: run-notary-004
completed_at: 2026-11-02T02:26:05.066Z
---

# Work Item: Read Notary settings from configuration so user secrets work

## Description

`src/web/Program.cs` fills `NotaryConfiguration` from process environment variables only (`SetPropertiesEnvVariable`). User secrets feed `IConfiguration`, not the environment, so a dev tenant stored in user secrets would be ignored. Move the binding into a testable class in the core library, fed by a lookup over `builder.Configuration`.

## Acceptance Criteria

- [ ] A `NOTARY_*` value held in user secrets (flat key such as `NOTARY_OPENID_DOMAIN`) reaches `config`, `config.Database` and `config.OpenId`
- [ ] An environment variable overrides the same key in user secrets (the default `IConfiguration` precedence)
- [ ] Unset or empty values leave the existing defaults untouched
- [ ] `Program.cs` uses the new class; the reflection loop in `Program.cs` is removed
- [ ] Behavior on the Pi is unchanged: the same environment variables still work
- [ ] Unit tests cover binding, override and default cases

## Technical Notes

Bind through a `Func<string, string?>` lookup so the tests need no `IConfiguration`. `src/setup/Program.cs` has its own copy of the loop and reads only the environment; leave it alone (out of scope). Allman braces, constructor injection, and NUnit like the existing tests in `src/test`.

## Dependencies

(none)
