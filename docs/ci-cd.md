# CI/CD

## CI (`.github/workflows/ci.yml`)

| Trigger | What runs |
| --- | --- |
| Pull request to `main` | Restore, Release build, tests with a line-coverage gate (65%, ratchet toward 70%) |
| Push to `main` (PR merge) | The same gates, then **release** |

Release (only after the gates pass on `main`):

1. Creates the git tag `v<NOTARY_MAJOR_VERSION>.<NOTARY_MINOR_VERSION>.<run_number>`.
2. Builds the multi-arch (`amd64`, `arm64`) image from `src/Dockerfile`.
3. Pushes `notary-web:<version>` and `notary-web:latest` to Docker Hub. `latest` always matches the newest tag.

Repository configuration:

- Secrets: `DOCKER_HUB_USER`, `DOCKER_HUB_TOKEN`
- Variables: `NOTARY_MAJOR_VERSION`, `NOTARY_MINOR_VERSION`
- Branch protection on `main`: require the **Build and Quality Gates** check to pass before merging
  (Settings > Branches > `main` > Require status checks).

## CD (`.github/workflows/deploy.yml`)

Run manually: **Actions > Deploy > Run workflow**, optionally entering a tag (`1.1.49` or `v1.1.49`). Blank means `latest`.
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
| `NOTARY_TAG` | No | Image tag; Deploy overrides it. Defaults to `latest` |
| `NOTARY_APP_KEY` | Yes | 16+ character passphrase protecting stored private keys. Back it up separately; never change it once keys exist ([details](private-key-storage.md)) |
| `NOTARY_DB_HOST` | No | MongoDB URL. Defaults to the bundled `mongo` service |
| `NOTARY_DB_USERNAME`, `NOTARY_DB_PASSWORD` | Yes | MongoDB credentials. Notary authenticates against the `admin` database; the bundled `mongo` creates this root user on first start |
| `NOTARY_DB_NAME` | No | Database name, default `notary` |
| `NOTARY_OPENID_DOMAIN`, `NOTARY_OPENID_CLIENTID`, `NOTARY_OPENID_CLIENTSECRET` | Yes | Auth0 application used to sign in |
| `NOTARY_CRL_ENDPOINT` | No | Public base URL for CRL distribution points, e.g. `https://pki.example.com` |

Notes:

- Auth0: create a Regular Web Application. Set Allowed Callback URLs to `https://<your-host>/callback` and Allowed Logout URLs to `https://<your-host>/`.
- First start: after editing `.env`, run `docker compose up -d` once by hand and check `docker compose logs notary-web`. The app refuses to start without a valid `NOTARY_APP_KEY`.
- `docker compose config` prints the resolved file and names any missing required variable.

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
