# Move the CA data from dev to production (MongoDB Atlas)

Copies the whole dev `notary` database to the production Atlas database, so the CA chain created in dev (see [initial-ca-bootstrap.md](initial-ca-bootstrap.md)) can issue and revoke in production.

The dump contains **encrypted private keys**. Treat it like a key file: restrict access, keep it only as long as needed, and delete it afterwards.

## What is copied

The whole database, not a list of collections: that includes the CA, key, certificate, revocation and CRL records (`certificate_authorities`, `crl_records` and the others the repositories create) and their indexes. Copying the lot means a collection added later cannot be missed.

## Prerequisites

- **Same `NOTARY_APP_KEY` in production as in dev.** Keys are encrypted with it. A different value leaves every stored key unreadable, and the startup log does not tell you: `Private key migration: ... 0 failed` only means the keys are in the encrypted format, not that they decrypt. Prove it with a signing test (step 6) and compare hashes (step 4).
- **Clean dev database.** Only the real chain should be in it; testing CAs would be copied too. Check in the UI before dumping.
- **Atlas user** with `readWrite` on the target database (`notary`, and the scratch database for the dry run). The same user and database go into `NOTARY_DB_USERNAME`, `NOTARY_DB_PASSWORD`, `NOTARY_DB_NAME` and `NOTARY_DB_HOST` in `/opt/notary/.env`. See [ci-cd.md](ci-cd.md) for Atlas setup.
- **Atlas network access**: the Pi's public IP is on the allow-list, and this PC's IP is allowed **temporarily** for the restore, then removed.
- Docker Desktop on this PC. No MongoDB tools need installing: the commands run from the `mongo` image.

Replace the placeholders below: `DEVHOST` (where dev MongoDB listens as seen from a container, usually `host.docker.internal:27017`), `DEVUSER`/`DEVPASS`, `ATLASHOST` (the `cluster0.xxxxx.mongodb.net` part), `ATLASUSER`/`ATLASPASS`.

## 1. Dump the dev database

Work in a directory that is not inside the repository and is not synced (not OneDrive or Dropbox):

```bash
mkdir -p ~/notary-migration && cd ~/notary-migration
docker run --rm -v "$PWD:/out" mongo:7 mongodump \
  --uri "mongodb://DEVUSER:DEVPASS@DEVHOST/?authSource=admin" \
  --db notary --archive=/out/notary-dev.archive --gzip
```

Check what is in the dump:

```bash
docker run --rm -v "$PWD:/out" mongo:7 mongorestore --archive=/out/notary-dev.archive --gzip --dryRun --verbose 2>&1 | grep -E "restoring|documents"
```

## 2. Dry run into a scratch database

Restore under a different database name so nothing real is touched. `--nsFrom/--nsTo` renames `notary` to `notary_dryrun`:

```bash
docker run --rm -v "$PWD:/out" mongo:7 mongorestore \
  --uri "mongodb+srv://ATLASUSER:ATLASPASS@ATLASHOST/?authSource=admin" \
  --archive=/out/notary-dev.archive --gzip \
  --nsFrom 'notary.*' --nsTo 'notary_dryrun.*'
```

The Atlas user needs `readWrite` on `notary_dryrun` for this.

Then prove it works: run a local Notary (Visual Studio or `dotnet run`) with `NOTARY_DB_HOST=mongodb+srv://ATLASHOST`, `NOTARY_DB_NAME=notary_dryrun`, the Atlas credentials and the **production** `NOTARY_APP_KEY`, and:

1. Read the startup log: `Private key migration: 0 migrated, ... 0 failed`. This is **not** proof the app key is right (it never decrypts); the CRL in step 3 below is.
2. Open the intermediate CA and issue a throwaway leaf certificate from it. This proves the stored CA key decrypts and signs.
3. Download that certificate's CRL URL (`/api/crl/...`) and check it with `openssl crl -inform DER -noout -text`.
4. Drop the scratch database in Atlas (Browse Collections > `notary_dryrun` > Drop Database).

Record the result:

| Check | Result | Date |
| --- | --- | --- |
| Restore finished, document counts match the dump | | |
| Startup log shows 0 failed (format only) | | |
| App key hash matches production `.env` (step 4) | | |
| Throwaway leaf issued from the intermediate | | |
| CRL downloads and parses | | |
| Scratch database dropped | | |

## 3. Restore into production

Only after the dry run passes. If `notary` already has data in Atlas (a failed earlier attempt), take a backup first (Atlas > Backup, or run the dump command against it) and add `--drop` to replace the collections:

```bash
docker run --rm -v "$PWD:/out" mongo:7 mongorestore \
  --uri "mongodb+srv://ATLASUSER:ATLASPASS@ATLASHOST/?authSource=admin" \
  --archive=/out/notary-dev.archive --gzip --nsInclude 'notary.*'
```

## 4. Point production at it

In `/opt/notary/.env` on the Pi set `NOTARY_DB_HOST`, `NOTARY_DB_NAME=notary`, the Atlas credentials, **the exact `NOTARY_APP_KEY` used in dev** (the template leaves it empty, so it is easy to miss) and `NOTARY_CRL_ENDPOINT=http://notary.unionsquared.lan`. In Compose a `$` in a value is interpolation: write `$$` for a literal `$`.

Then check the Pi has the same key without printing it. The two hashes must be identical:

```bash
# Pi
sudo docker compose exec notary-web sh -c 'printf %s "$NOTARY_APP_KEY" | sha256sum'
```

```powershell
# dev PC, using the value the dev Notary ran with
$bytes = [Text.Encoding]::UTF8.GetBytes($k)
([BitConverter]::ToString([Security.Cryptography.SHA256]::HashData($bytes)) -replace '-','').ToLower()
```

Then `docker compose up -d --force-recreate notary-web`.

## 5. Clean up

- Delete the dump: `shred -u ~/notary-migration/notary-dev.archive` (on Windows, delete it and empty the recycle bin).
- Remove this PC's IP from the Atlas allow-list.
- Do not put the archive in the repository, a chat, or a shared drive. If you must keep one, keep it encrypted (for example `age` or an encrypted archive) with the password stored separately.

## 6. Verify production

1. Fetch a CRL, which makes the app decrypt the CA key and sign: `curl -s http://notary.unionsquared.lan/api/crl/<id> | openssl crl -inform DER -noout -issuer -nextupdate`. A `pad block corrupted` error in `docker compose logs notary-web` means the app key differs from dev.
2. Sign in at `https://notary.unionsquared.lan`; the root, intermediate and leaf appear.
3. Issue a test certificate from the intermediate, check it, then revoke it and confirm the CRL at `http://notary.unionsquared.lan/api/crl/<issuer>` lists it.

## Rollback

Production is unchanged until step 4, so abandoning before it costs nothing: drop the scratch database and delete the dump. After step 4, point `.env` back at the previous database (or stop the stack); the dev database is untouched by the whole procedure and remains the source of truth until you are satisfied.

## Gaps

There is no import/export tool in the app. If this procedure proves unsafe or incomplete, open an issue for one rather than working around it.
