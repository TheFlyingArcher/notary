# Code Review: run-notary-004

## Work Item: config-value-source

Files reviewed: `NotaryConfigurationBinder.cs`, `NotaryConfigurationBinderTest.cs`, `Program.cs`.

- Allman braces, no service locator, no static state. The binder is a pure function of its inputs.
- Behavior preserved from the old loop: empty values are skipped and only the attributed properties are set. A null section (the `NotaryConfiguration` copy constructor leaves `OpenId` null) is skipped instead of throwing.
- Removed the now-unused `using System.Reflection;` from `Program.cs`.
- No auto-fixes needed. No findings.

---

## Work Item: openid-tenant-guard

Files reviewed: `OpenIdTenantGuard.cs`, `OpenIdTenantGuardTest.cs`, `NotaryOpenIdConfiguration.cs`, `Program.cs`, `deploy/docker-compose.yml`, `deploy/.env.example`.

- The guard is a single-purpose static class with no state, called once at startup. Allman braces throughout.
- Label matching is case-insensitive and trims whitespace. The tenant label is not a secret, so echoing it in the message is fine; the client secret is never touched.
- Operational note, not a defect: the Pi `.env` needs `NOTARY_OPENID_TENANT=production` before this release is deployed, or compose refuses to start the container. The docs item records this.
- No auto-fixes needed. No findings.

---

## Work Item: auth0-client-secret

Files reviewed: `NotaryOpenIdConfiguration.cs`, `NotaryConfiguration.cs`, `Program.cs`, `NotaryConfigurationTest.cs`.

- `Validate()` lives with the settings it checks and is called from `NotaryConfiguration.Validate()` through a null-safe call, because the copy constructor leaves `OpenId` null.
- Messages name the variable only. The secret is never read into a message.
- Behavior change to call out in the docs: a dev setup with a domain and client ID but no secret now fails at startup instead of signing in without code flow.
- The Auth0 SDK's docs mention the secret only for API access tokens, so real sign-in is proven by UAT, not by unit tests.
- No auto-fixes needed. No findings.

---

## Work Item: auth0-tenant-docs-uat

Files reviewed: `docs/ci-cd.md`, `docs/initial-ca-bootstrap.md`.

- No real domains, client IDs or secrets appear; the commands use placeholders.
- The new section is placed before NGINX and HTTPS and linked from the env table and the Auth0 note.
- `dotnet user-secrets list` prints values, so the docs say not to run it on a shared screen.
- No auto-fixes needed. No findings.
