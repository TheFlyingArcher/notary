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

### Pi-side compose file

`PI_DEPLOY_PATH` must contain a `docker-compose.yml` that references the tag variable. Minimal example (add your own environment, volumes and NGINX service; see #50 for TLS):

```yaml
services:
  notary-web:
    image: <dockerhub-user>/notary-web:${NOTARY_TAG:-latest}
    restart: unless-stopped
    ports:
      - "8080:80"
```

Roll back by running Deploy with the previous tag.
