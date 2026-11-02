# CI/CD

## CI (`.github/workflows/ci.yml`)

| Trigger | What runs |
| --- | --- |
| Pull request to `main` | If .NET inputs changed: restore, Release build, tests with a line-coverage gate (65%, ratchet toward 70%) |
| Push to `main` (PR merge) | If .NET inputs changed: the same gates, then **release** |

Only changes that can affect the .NET code trigger the build, tests and release (see [Selective runs](#selective-runs)).

Release (only after the gates pass on `main`):

1. Creates the git tag `v<NOTARY_MAJOR_VERSION>.<NOTARY_MINOR_VERSION>.<run_number>`.
2. Builds the multi-arch (`amd64`, `arm64`) image from `src/Dockerfile`.
3. Pushes `notary-web:<version>` and `notary-web:latest` to Docker Hub. `latest` always matches the newest tag.

Repository configuration:

- Secrets: `DOCKER_HUB_USER`, `DOCKER_HUB_TOKEN`, `CHANGELOG_PUSH_TOKEN` (see [Change log](#change-log-githubworkflowschangelogyml))
- Variables: `NOTARY_MAJOR_VERSION`, `NOTARY_MINOR_VERSION`
- Branch protection on `main`: require the **Build and Quality Gates** check to pass before merging
  (Settings > Branches > `main` > Require status checks). Also require **Detect Changes** (see [Selective runs](#selective-runs)).

## Selective runs

`ci.yml` and `codeql.yml` each start with a small **Detect Changes** job that runs [`.github/scripts/detect-dotnet-changes.sh`](../.github/scripts/detect-dotnet-changes.sh). It compares the pull request with its base (or a push with the commit before it) and reports whether any .NET input changed. The heavy jobs run only if it did:

| Runs when the change touches | Skipped for |
| --- | --- |
| `src/` (code, tests, `Dockerfile`, `CHANGELOG.md`), `Notary.AppHost/`, `Notary.ServiceDefaults/` | `docs/`, `deploy/`, `README.md` |
| `Directory.*.props`/`.targets`, `global.json`, `nuget.config`, `.editorconfig`, `.globalconfig` anywhere | `.specs-fire/`, `.specsmd/`, `.claude/`, `.vscode/` |
| `.github/workflows/ci.yml`, `.github/workflows/codeql.yml`, the detection script | other workflows (`deploy.yml`, `changelog.yml`), issue and PR templates |

What this means:

- **A skipped job counts as passing.** A docs-only pull request shows **Build and Quality Gates** and **Analyze (csharp)** as skipped and can still merge. A workflow-level `paths:` filter would not do this: the required check would stay pending forever.
- **No code change, no release.** `release` needs `quality`, so a merge that changed no .NET inputs creates no tag and no image.
- **When in doubt, everything runs.** Scheduled and manual runs, a first push, a missing commit or a failed diff all count as "changed".
- **If Detect Changes fails, the gates are skipped and look green.** That is why **Detect Changes** should be a required check as well; the script itself never fails because of a diff problem.
- **To change what counts**, edit the `RELEVANT` pattern at the top of the script. Both workflows use it, and a change to the script runs everything.

## FIRE state check (`.github/workflows/fire-state.yml`)

The `.specs-fire/` folder is the planning record. Its `state.yaml` and the `brief.md` and work item files must agree, or finished work stays marked `in_progress`. This workflow runs [`check-fire-state.cjs`](../.github/scripts/check-fire-state.cjs) and fails a pull request when:

- an intent is not `completed` (with `completed_at`) although every one of its work items is `completed`, or is `completed` while an item is not
- a `brief.md` or work item file has a different `status` than `state.yaml`, or a completed brief has no `completed_at`

It runs only when `.specs-fire/**`, the script, its tests or the workflow change, so build, test and CodeQL are still skipped for those pull requests. It is **not** a required check: a workflow filtered out by `paths:` leaves a required check pending on every other pull request.

To fix a failure, run this from the repository root (it needs the `yaml` package, for example `npm install --no-save yaml`) and commit the result:

```bash
node .github/scripts/check-fire-state.cjs --fix
```

`--fix` only changes `status` and `completed_at` values; `state.yaml` is the source of truth for work item statuses. Run it after the last run of an intent, because the FIRE run scripts do not close the intent in `state.yaml`. The script's tests run with `node --test ".github/scripts/tests/*.test.cjs"`.
## CodeQL (`.github/workflows/codeql.yml`)

Scans C# on pull requests (only when .NET inputs changed), on every push to `main`, every Monday at 03:17 UTC, and on demand. The push and weekly scans mean a skipped pull request scan never leaves the code unchecked. Results appear under Security > Code scanning.

This workflow replaces GitHub's **Default setup**, which cannot be limited to paths. Turn Default setup off (Settings > Advanced Security > Code scanning > CodeQL analysis > **Switch to advanced** / disable Default setup), otherwise every pull request is scanned twice. The new job is also named `Analyze (csharp)`, so a required check with that name keeps working.

### Checking the setup (UAT)

1. In Settings, confirm CodeQL Default setup is off, and that **Build and Quality Gates**, **Analyze (csharp)** (if you require it) and **Detect Changes** are required checks.
2. Open a pull request that changes only a file under `docs/`. Expect `Detect Changes` to run, **Build and Quality Gates** and **Analyze (csharp)** to be skipped, and the pull request to be mergeable.
3. Open a pull request that changes a file under `src/`. Expect the build, tests, coverage gate and CodeQL scan to run.
4. Merge the docs-only pull request. Expect no new `v*` tag and no new image.
5. Merge the code pull request. Expect the tag and image as before.
6. Run Actions > CodeQL > **Run workflow** once. Manual runs always scan, which also proves the full scan works.

## Change log (`.github/workflows/changelog.yml`)

The home page shows [`src/web/CHANGELOG.md`](../src/web/CHANGELOG.md), newest release first. The workflow keeps it current: when you **publish** a GitHub Release, it adds that release's version, date and notes to the top of the file and pushes the commit straight to `main`.

How it behaves:

- **Only published releases appear.** A merge to `main` creates a tag but not a release, so unreleased work is not in the change log. Prereleases are skipped too.
- **No tag, no image.** The commit is `Add vX.Y.Z to the change log [skip ci]`. `[skip ci]` stops CI from running for it, and the CI `release` job also refuses a `[skip ci]` commit, so it never creates a tag or builds an image.
- **The deployed app is one release behind.** The image for `v1.2.20` is built when its tag is created, before you publish the release, so it contains the change log up to `v1.2.19`. Its own entry is in `main` straight away but reaches a running app only in the next image. The file in `main` is always current.
- **Release notes are cleaned.** The "Full Changelog" link is dropped, "by @user in <PR URL>" becomes "(#PR)", and every heading in the notes is made level 4 or deeper, so a note cannot add an entry of its own. An empty release gets `_No release notes._`.
- **Safe to re-run.** If the version is already in the file, nothing is committed. To redo a missed release, use **Actions > Changelog > Re-run** on its run, or edit the file in a pull request (keep the `## vX.Y.Z - yyyy-MM-dd` heading format described at the top of the file).

### One-time setup: the push token

`main` is protected by a ruleset (Settings > Rules > Rulesets > `main`) that requires a pull request. The workflow's built-in `GITHUB_TOKEN` cannot bypass that, so the workflow pushes with a token you create, stored as the secret `CHANGELOG_PUSH_TOKEN`.

1. **Create a fine-grained personal access token.** GitHub > your avatar > Settings > Developer settings > Personal access tokens > Fine-grained tokens > **Generate new token**.
   - Token name: `notary-changelog`
   - Resource owner: your account
   - Expiration: the longest you are comfortable with (up to one year). Put a reminder in your calendar, because the workflow fails when the token expires.
   - Repository access: **Only select repositories**, then `TheFlyingArcher/notary`
   - Permissions > Repository permissions: **Contents: Read and write**. Leave everything else at "No access" (Metadata read-only is added for you).
2. Click **Generate token** and copy it now; GitHub shows it only once.
3. **Add it as a secret.** In the repository: Settings > Secrets and variables > Actions > **New repository secret**. Name `CHANGELOG_PUSH_TOKEN`, value the token you copied.
4. **Let it bypass the ruleset.** Settings > Rules > Rulesets > `main` > Bypass list > **Add bypass** > **Repository admin** > Bypass mode **Always** > Save. The token acts as you, and you are the repository admin, so this is what lets its direct push through. The ruleset still blocks deleting `main` and force pushes for everyone.
5. **Check it.** Publish a release and open **Actions > Changelog**. `main` should gain an `Add vX.Y.Z to the change log [skip ci]` commit, no new `v1.2.N` tag should appear, and CI should not run for that commit.

If the **Checkout Repository** step fails with a 401 or 403, the secret is missing, wrong or expired. If **Commit and Push** fails with a "protected branch" or "changes must be made through a pull request" error, the bypass in step 4 is not in place.

Keep the token to this one repository and to Contents access. Anyone who can read the secret can push to `main`, so do not widen who can edit workflows or secrets, and rotate it (repeat steps 1 to 3) if you think it has leaked.

## CD (`.github/workflows/deploy.yml`)

Run manually: **Actions > Deploy > Run workflow** and enter a release version (`1.2.14` or `v1.2.14`). The tag is required and must be a published GitHub Release (Releases > Draft a new release on an existing `v<version>` tag); a merge to `main` creates a tag but not a release, so unreleased builds cannot be deployed.
The runner joins your tailnet with Tailscale, SSHes to the Pi, and runs `docker compose pull && up -d` with `NOTARY_TAG` set.

### One-time Tailscale setup

Assumes `tailscaled` is already running and logged in on the Pi.

1. **Tag the Pi.** In the admin console (Machines > the Pi > Edit ACL tags) or via `sudo tailscale up --advertise-tags=tag:pi`, give it `tag:pi`. Note its MagicDNS name (e.g. `raspberrypi`).
2. **Define tags and an ACL** (Access controls > edit the policy file):
   ```json
   {
     "tagOwners": {
       "tag:ci": ["autogroup:admin"],
       "tag:pi": ["autogroup:admin"]
     },
     "acls": [
       { "action": "accept", "src": ["tag:ci"], "dst": ["tag:pi:22"] }
     ]
   }
   ```
   Merge these into your existing policy rather than replacing it.
3. **Create an OAuth client** (Settings > Trust credentials > Credential > OAuth, scope `auth_keys` write, tag `tag:ci`). Copy the client ID and secret.
4. **Create a dedicated deploy user and SSH key.**
   1. On the Pi, create a user for deployments only (no password, no sudo) and a directory for the compose file:
      ```bash
      sudo adduser --disabled-password --gecos "" notary-deploy
      sudo usermod -aG docker notary-deploy
      sudo mkdir -p /opt/notary
      sudo chown notary-deploy:notary-deploy /opt/notary
      ```
      `docker` group membership is effectively root on the Pi. A dedicated user keeps the CI key away from your personal account and files. Use `PI_USER=notary-deploy` and `PI_DEPLOY_PATH=/opt/notary` below.
   2. On your machine, generate a key pair:
      ```bash
      ssh-keygen -t ed25519 -f notary-deploy -N "" -C "github-actions-notary"
      ```
   3. On the Pi, install the public key (paste the contents of `notary-deploy.pub`):
      ```bash
      sudo -u notary-deploy mkdir -p -m 700 /home/notary-deploy/.ssh
      echo "<contents of notary-deploy.pub>" | sudo -u notary-deploy tee -a /home/notary-deploy/.ssh/authorized_keys
      sudo chmod 600 /home/notary-deploy/.ssh/authorized_keys
      ```
   4. Test from a machine on your tailnet: `ssh -i notary-deploy notary-deploy@<PI_HOST> "docker ps"`.
   5. Copy the contents of the private file `notary-deploy` into the `PI_SSH_PRIVATE_KEY` secret (next step), then delete both local key files.
5. **Add to GitHub** (Settings > Secrets and variables > Actions):
   - Secrets: `TS_OAUTH_CLIENT_ID`, `TS_OAUTH_SECRET`, `PI_SSH_PRIVATE_KEY` (contents of `notary-deploy`)
   - Variables: `PI_HOST` (the Pi's Tailscale MagicDNS name or `100.x` tailnet IP, **not** a LAN address, since the runner reaches the Pi only through the tailnet), `PI_USER`, `PI_DEPLOY_PATH` (directory holding the compose file)

### Pi-side compose file and environment

`PI_DEPLOY_PATH` (e.g. `/opt/notary`) must contain a `docker-compose.yml` and a `.env`. Both templates are in [`deploy/`](../deploy):

```bash
# On the Pi, as notary-deploy
cd /opt/notary
# copy deploy/docker-compose.yml and deploy/.env.example here (scp, or paste), then:
mv .env.example .env
chmod 600 .env
nano .env
```

The stack runs `notary-web` (published only on `127.0.0.1:8080` for NGINX, see #50). The optional `mongo` container (no published ports, data in the `notary-db` volume) only starts with `--profile local-db`; see "Database" below.

| Variable | Required | Purpose |
| --- | --- | --- |
| `DOCKER_HUB_USER` | Yes | Docker Hub account that owns `notary-web` |
| `NOTARY_TAG` | Yes | Release version, e.g. `1.2.14`. Deploy overrides it; keep it set to the running release because there is no default |
| `NOTARY_APP_KEY` | Yes | 16+ character passphrase protecting stored private keys. Back it up separately; never change it once keys exist ([details](private-key-storage.md)) |
| `NOTARY_DB_HOST` | No | MongoDB URL. Defaults to the bundled `mongo` service |
| `NOTARY_DB_USERNAME`, `NOTARY_DB_PASSWORD` | Yes | MongoDB credentials. Notary authenticates against the `admin` database; the bundled `mongo` creates this root user on first start |
| `NOTARY_DB_NAME` | No | Database name, default `notary` |
| `NOTARY_OPENID_DOMAIN`, `NOTARY_OPENID_CLIENTID`, `NOTARY_OPENID_CLIENTSECRET` | Yes | The **production** Auth0 application used to sign in. See [Auth0 tenants](#auth0-tenants) |
| `NOTARY_OPENID_TENANT` | Yes | `production` on the Pi. Notary refuses to start if it does not match the environment. **Add it before deploying the release that introduced it** |
| `NOTARY_CRL_ENDPOINT` | No | Public base URL for CRL distribution points, set before issuing, it is baked into each certificate. Use `http://` (e.g. `http://notary.unionsquared.lan`): CRLs are fetched over plain HTTP so revocation checks are not circular |

Notes:

- Auth0: use a separate production tenant, set up as described in [Auth0 tenants](#auth0-tenants).
- First start: after editing `.env`, run `docker compose up -d` once by hand and check `docker compose logs notary-web`. The app refuses to start without a valid `NOTARY_APP_KEY`.
- `docker compose config` prints the resolved file and names any missing required variable.

### Auth0 tenants

Notary uses two Auth0 tenants so that work on the development tenant (callback URLs, rules, users, deleting an application) can never break production sign-in:

| | Development tenant | Production tenant |
| --- | --- | --- |
| Used by | Visual Studio and `dotnet run` on your PC | Notary on the Pi |
| Settings come from | `dotnet user-secrets` on `src/web` | `/opt/notary/.env` |
| `NOTARY_OPENID_TENANT` | `development` | `production` |
| Allowed Callback URLs | `https://localhost:17244/callback` | `https://notary.unionsquared.lan/callback` |
| Allowed Logout URLs | `https://localhost:17244/` | `https://notary.unionsquared.lan/` |

For each tenant create a **Regular Web Application**, then copy its domain, client ID and client secret. Leave Application Login URI blank (Auth0 rejects the `.lan` name) and leave Allowed Web Origins and Allowed Origins (CORS) blank: Notary signs in on the server. Do not put localhost URLs on the production tenant, or the production URL on the development tenant.

A startup guard enforces the split. When `NOTARY_OPENID_DOMAIN` is set, `NOTARY_OPENID_TENANT` must be `development` or `production`. `development` is accepted only when `ASPNETCORE_ENVIRONMENT` is `Development`; `production` is accepted in every other environment. Anything else stops Notary at startup with a message naming the setting. A domain also needs a client ID and a client secret. No message ever includes the secret.

**Production (the Pi).** Before deploying the release that adds the guard, add this line to `/opt/notary/.env`, or `docker compose up` refuses to start:

```bash
NOTARY_OPENID_TENANT=production
```

Set the production tenant's domain, client ID and client secret in the same file (see `deploy/.env.example`), then run `docker compose config` to check that no variable is missing.

**Local development.** Store the development tenant's values in user secrets, which live outside the repository. Run these from the repository root and type the real values yourself:

```bash
dotnet user-secrets set NOTARY_OPENID_DOMAIN "<dev-tenant>.us.auth0.com" --project src/web
dotnet user-secrets set NOTARY_OPENID_CLIENTID "<dev client id>" --project src/web
dotnet user-secrets set NOTARY_OPENID_CLIENTSECRET "<dev client secret>" --project src/web
dotnet user-secrets set NOTARY_OPENID_TENANT "development" --project src/web
```

`dotnet user-secrets list --project src/web` shows what is stored (it prints values, so do not run it on a shared screen). A real environment variable with the same name overrides the user secret. Remove the old `NOTARY_OPENID_*` environment variables from your PC if you set them, so they do not shadow the secrets.

**UAT checklist.**

- [ ] **Dev sign-in.** Run the `https` profile in Visual Studio. Sign-in goes to the development tenant's domain and returns to `https://localhost:17244`; sign-out returns to the app.
- [ ] **Wrong tenant is refused.** Set `NOTARY_OPENID_TENANT` to `production` in user secrets and start the app: it stops with a message naming `NOTARY_OPENID_TENANT`, `production` and `Development`. Set it back to `development`.
- [ ] **Missing label is refused.** Remove `NOTARY_OPENID_TENANT` and start the app: it stops with "must be 'development' or 'production'". Restore it.
- [ ] **Missing credentials are refused.** Remove `NOTARY_OPENID_CLIENTSECRET` and start the app: it stops with a message naming `NOTARY_OPENID_CLIENTSECRET`. Restore it.
- [ ] **Pi pre-deploy.** `/opt/notary/.env` has `NOTARY_OPENID_TENANT=production` and the production tenant's values; `docker compose config` shows no missing variable.
- [ ] **Pi sign-in.** After the deploy, `docker compose logs notary-web` shows no OpenID or Auth0 errors, and sign-in and sign-out work at `https://notary.unionsquared.lan`.
- [ ] **Tenants stay apart.** The production tenant has no localhost URLs and the development tenant has no `notary.unionsquared.lan` URLs.

### NGINX and HTTPS

The Pi's host NGINX (shared with other projects) terminates TLS and proxies to `notary-web` on `127.0.0.1:8080`. Notary adds one site; the others are untouched.

- Site config: [`deploy/nginx/notary.conf`](../deploy/nginx/notary.conf) goes to `/etc/nginx/sites-available/notary`, linked into `sites-enabled/`.
- Certificate files: `/etc/nginx/certs/notary/fullchain.pem` (leaf then intermediate, `644`) and `privkey.pem` (`600`, root-owned).
- The CA chain, the certificate files, DNS, Auth0 and trusting the root are covered step by step in [initial-ca-bootstrap.md](initial-ca-bootstrap.md); moving the CA data to Atlas is in [ca-data-migration.md](ca-data-migration.md).

```bash
sudo cp deploy/nginx/notary.conf /etc/nginx/sites-available/notary
sudo ln -sf /etc/nginx/sites-available/notary /etc/nginx/sites-enabled/notary
sudo nginx -t && sudo systemctl reload nginx
```

**The Deploy workflow only changes the image tag** (`docker compose pull && up -d`). It never updates `docker-compose.yml`, `.env` or the NGINX site. When those change in the repository, copy them by hand:

- `deploy/docker-compose.yml` to `/opt/notary/docker-compose.yml`, then `docker compose up -d`.
- `deploy/nginx/notary.conf` to `/etc/nginx/sites-available/notary`, then `sudo nginx -t && sudo systemctl reload nginx`.

### Database

Notary needs MongoDB 5.0 or later (it requests Stable API v1). The official 5.0+ builds require the ARMv8.2-A CPU feature set, so:

| Host | What to do |
| --- | --- |
| Raspberry Pi 5, or any x64 machine | Run the bundled database: `docker compose --profile local-db up -d` |
| **Raspberry Pi 4 (Cortex-A72, ARMv8.0)** | MongoDB cannot run on it. Use [MongoDB Atlas](#using-mongodb-atlas) (below), or run MongoDB on a separate x64 or Pi 5 host on your tailnet and set `NOTARY_DB_HOST=mongodb://<tailnet-ip>:27017`. Do not start the `local-db` profile |

Check a Pi with `lscpu`: no `atomics` in the flags means MongoDB 5.0+ will not run. Running Postgres on the Pi 4 instead of MongoDB is tracked in [#59](https://github.com/TheFlyingArcher/notary/issues/59).

When the database is on another host, allow only the Pi to reach its port (Tailscale ACL, for example `tag:pi` to `<db-host>:27017`) and enable authentication and TLS on it.

### Using MongoDB Atlas

The free M0 tier works for a home PKI. Notary encrypts every stored private key with `NOTARY_APP_KEY`, which never leaves the Pi, so the database alone does not expose usable CA keys. Certificate metadata is still stored there.

1. **Create a cluster.** At [cloud.mongodb.com](https://cloud.mongodb.com) create a project and a free **M0** cluster. Pick a region near you. The cluster must run MongoDB 5.0 or later (the default does).
2. **Create a database user.** Security > Database Access > Add New Database User, password authentication, with a long random password. Under Specific Privileges, grant the built-in role **`readWrite`** on the **`notary`** database only. Notary needs `readWrite` to create its indexes and to read and write documents. `dbAdmin` alone is not enough: it cannot read or write documents, so the app would start and then fail with "not authorized". Do not use `atlasAdmin` or "read and write to any database". Atlas stores the user in the `admin` database, which is where Notary authenticates (hard-coded in `IocRegistration.cs`), so this works as is. Notary passes the password separately, so it needs no URL-encoding. Use a password user: AWS IAM, OIDC and X.509 users do not work with Notary.
3. **Allow the Pi.** Security > Network Access > Add IP Address, and enter your home network's **public** IP (what the Pi appears as to the internet). Do not use `0.0.0.0/0`. If your ISP changes the address, update this entry or connections will time out.
4. **Get the connection string.** Cluster > Connect > Drivers. Copy only the host part, for example `mongodb+srv://cluster0.abcde.mongodb.net`. Leave out the username, password and any query options, since Notary supplies the credentials itself.
5. **Set `.env` on the Pi:**
   ```bash
   NOTARY_DB_HOST=mongodb+srv://cluster0.abcde.mongodb.net
   NOTARY_DB_USERNAME=<atlas user>
   NOTARY_DB_PASSWORD=<atlas password>
   NOTARY_DB_NAME=notary   # must match the database the user's role is scoped to
   ```
   Do not start the `local-db` profile.
6. **Check it.** Run `docker compose up -d` and `docker compose logs -f notary-web`. A connection timeout usually means the IP allow-list is wrong. An authentication error means the username or password is wrong.

**Backups.** M0 has no automated backups. Take your own export regularly and store it somewhere safe. It holds encrypted keys, so protect it like the database:

```bash
mongodump --uri "mongodb+srv://<user>@cluster0.abcde.mongodb.net/notary" --out notary-backup-$(date +%F)
```

Back up `NOTARY_APP_KEY` separately. Without it, a restored backup cannot be read.

Atlas compatibility has not been verified against Notary yet (see the note in `src/library/data/IocRegistration.cs`). Test it before relying on it.

Roll back by running Deploy with the previous tag.
