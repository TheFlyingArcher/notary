<!--
Notary change log. Newest release first. Read by the homepage and updated by .github/workflows/changelog.yml.

Format: each release starts with a level-2 heading, exactly
    ## v<major>.<minor>.<patch> - <yyyy-MM-dd>
followed by Markdown release notes. Only that heading starts a new entry, so headings inside the
notes are level 4 or deeper.
-->

# Changelog

## v1.2.24 - 2026-11-02

#### Highlights since v1.2.20

#### Sign-in: separate Auth0 tenants
- **Development and production now use different Auth0 tenants**, so work on the development tenant can no longer break production sign-in. (#95)
- **New setting `NOTARY_OPENID_TENANT`** (`development` or `production`). Notary refuses to start when it does not suit `ASPNETCORE_ENVIRONMENT`: `development` is accepted only in the Development environment, `production` everywhere else. A missing or unknown label also stops startup. No message includes the client secret.
- **Settings can now come from user secrets** as well as environment variables, so local development no longer needs machine-wide environment variables. An environment variable still wins over a user secret.
- **The Auth0 client secret is now used for sign-in.** A domain without a client ID or client secret stops startup with a message naming the missing setting.
- `docs/ci-cd.md` has a new "Auth0 tenants" section with the `dotnet user-secrets` commands, the callback and logout URLs for each tenant, and a UAT checklist.

#### CA detail page
- **Info panel and Issued Certificates table on every CA.** The Info panel shows the CA kind with separate Not Before and Not After rows. A root CA lists its child CAs, any other CA lists its leaf certificates, each with status and View/Download. (#88)
- **The Issuer Tree is hidden on root CAs.** Elsewhere it runs root, CA, certificate, fully expanded, with CA purpose icons. (#88)
- **Tighter layout.** Info and the CRL panel sit side by side, and the Issuer Tree sits beside Issued Certificates. Panels stack on narrow screens and stretch to equal height. (#90)

#### Build and CI
- **The .NET build, tests and CodeQL run only when a change can affect the .NET code.** Docs-only and workflow-only merges publish no tag or image. CodeQL is now an advanced workflow. (#91)
- **New FIRE State check on pull requests** that touch `.specs-fire/`, with a `--fix` mode. (#93)
- The FIRE planning records were brought up to date. (#92)

#### Upgrade notes for the Raspberry Pi
- **Before deploying, `/opt/notary/.env` must contain `NOTARY_OPENID_TENANT=production`**, together with the production Auth0 tenant's domain, client ID and client secret. Without it `docker compose up` refuses to start. `deploy/docker-compose.yml` changed, so copy it to the Pi.
- Run Deploy with `v1.2.24` (Actions > Deploy > Run workflow).
- After the deploy, check `docker compose logs notary-web` for Auth0 errors and confirm sign-in and sign-out at `https://notary.unionsquared.lan`.
- Roll back by running Deploy with `v1.2.23`. The older compose file does not read the new setting, so it still starts.

## v1.2.20 - 2026-10-29

#### Highlights since v1.2.19

#### Home page
- **The home page is now the Notary change log.** The three certificate statistic tiles and the placeholder tile are gone. The page lists each release, newest first, with its notes, back to v1.0.15. (#87)
- The change log is read from `src/web/CHANGELOG.md`, which ships with the app. If the file is missing, the page shows a message instead of an error.

#### Release automation
- **Publishing a GitHub Release now adds it to the change log automatically.** The Changelog workflow commits the entry to `main` as `Add vX.Y.Z to the change log [skip ci]`. It creates no tag and builds no image. Prereleases are skipped. (#87)
- CI's release job also refuses a `[skip ci]` commit, so the change log commit can never create a tag.
- `docs/ci-cd.md` explains the workflow and how to set up the `CHANGELOG_PUSH_TOKEN` secret and the ruleset bypass.

#### Upgrade notes for the Raspberry Pi
- Run Deploy with `v1.2.20` (Actions > Deploy > Run workflow). No change to `deploy/docker-compose.yml` or the NGINX site, so nothing needs copying by hand.
- The running app's change log is one release behind: this release's own entry reaches the app in the next image.
- Roll back by running Deploy with `v1.2.19`.

## v1.2.19 - 2026-10-29

#### Highlights since v1.2.14

#### Certificate download
- **A CA's private key can no longer be exported as PKCS #12.** The service refuses it, and the download dialog doesn't offer the bundle for a CA. DER, PEM and PKCS #7 downloads of a CA still work. Anyone who exported a CA key through the `.pfx` option now gets an error instead. (#83)
- **Redesigned download dialog.** Friendly format names with a short description of the selected format, password fields only for PKCS #12, an optional file name (extension added and invalid characters stripped), and tests that no format exports the plaintext private key. (#83)
- **PEM-encoded PKCS #7 chain export.** New "PKCS #7 (PEM, chain)" option alongside "PKCS #7 (DER, chain)", both downloaded as `.p7b`. The PEM form works with `openssl pkcs7` as is, where the DER file needs `-inform DER`. (#81)

#### Mixed RSA/EC chains
- A child CA can now use a different key algorithm, curve and size than its parent. (#79)
- Notary refuses to issue anything weaker than the CA that issues it, compared by NIST SP 800-57 security bits (P-256 = 128, P-384 = 192, P-521 = 256, RSA-3072 = 128). Equal strength across types is allowed.
- The Create CA and Create Certificate pages default to the issuer's key and only offer options that meet the floor. Existing chains keep loading and validating. See `docs/key-strength.md`.

#### Deployment
- **Only published GitHub Releases can be deployed.** The Deploy workflow fails unless `v<version>` is a published release. (#78)
- `deploy/docker-compose.yml` no longer defaults `NOTARY_TAG` to `latest`.

#### Upgrade notes for the Raspberry Pi
- Run Deploy with `v1.2.19` (Actions > Deploy > Run workflow). Deploy exports `NOTARY_TAG` itself, so it is unaffected by the compose change.
- Before running `docker compose` by hand on the Pi, copy the new `deploy/docker-compose.yml` to `/opt/notary` and set `NOTARY_TAG=1.2.19` in `/opt/notary/.env`, or compose will refuse to start.
- Roll back by running Deploy with `v1.2.14`.

## v1.2.14 - 2026-10-26

First production release of Notary on the Raspberry Pi behind HTTPS with a private CA.

**Highlights**
- HTTPS via host NGINX with a private CA chain (EC P-384), plus plain-HTTP CRL endpoints; see `docs/initial-ca-bootstrap.md` and `docs/ca-data-migration.md`.
- CI/CD: image builds for amd64 and arm64 on every merge to `main`, manual Deploy workflow to the Pi (`docs/ci-cd.md`).
- PKCS #12 export now uses AES-256 / PBKDF2-SHA256 / HMAC-SHA256 and includes the full chain; PKCS #7 chain export added.
- Correct EC certificates: named-curve keys, signature hash matched to the issuer, no key/data encipherment on EC keys.
- Fixed the Docker image missing `blazor.server.js` and the NGINX header buffers for the OpenID callback.
- Data Protection keys persist across restarts.

**Deploying:** run the Deploy workflow with tag `1.2.14`. Copy `deploy/docker-compose.yml` and `deploy/nginx/notary.conf` to the Pi by hand when they change (Deploy only updates the image tag).

#### What's Changed
* Persist Data Protection keys in compose; drop redundant ASPNETCORE_URLS (#62)

## v1.1.29 - 2025-04-20

#### What's Changed
* Cleaning up unused code. Handling warnings. (#24)

## v1.1.19 - 2024-02-06

#### What's Changed
* Add CRL support. Key Usages. (#8)

## v1.0.18 - 2024-01-30

#### What's Changed
* Certificate Revocation, Bug Fixes, and Certificate Detail (#7)
* Migrating away from Azure AD authentication to Auth0 authentication.
* Finishing out certificate detail pages
* Refactoring some of the certificate download
* Adding light/dark mode depending on user's configuration

## v1.0.15 - 2024-01-20

Undoing HTTPS redirection within the app itself. Relying on K8s for redirection.
