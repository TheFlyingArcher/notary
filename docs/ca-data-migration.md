# Move the CA data from dev to production (MongoDB Atlas)

Copies the whole dev `notary` database to the production Atlas database, so the CA chain created in dev (see [initial-ca-bootstrap.md](initial-ca-bootstrap.md)) can issue and revoke in production.

The dump contains **encrypted private keys**. Treat it like a key file: restrict access, keep it only as long as needed, and delete it afterwards.

## What is copied

The whole database, not a list of collections: that includes the CA, key, certificate, revocation and CRL records (`certificate_authorities`, `crl_records` and the others the repositories create) and their indexes. Copying the lot means a collection added later cannot be missed.

## Prerequisites

- **Same `NOTARY_APP_KEY` in production as in dev.** Keys are encrypted with it. A different value leaves every stored key unreadable. Confirm after the move (step 6).
- **Clean dev database.** Only the real chain should be in it; testing CAs would be copied too. Check in the UI before dumping.
- **Atlas user** with `readWrite` on the target database (`notary`, and the scratch database for the dry run). The same user and database go into `NOTARY_DB_USERNAME`, `NOTARY_DB_PASSWORD`, `NOTARY_DB_NAME` and `NOTARY_DB_HOST` in `/opt/notary/.env`. See [ci-cd.md](ci-cd.md) for Atlas setup.
- **Atlas network access**: the Pi's public IP is on the allow-list, and this PC's IP is allowed **temporarily** for the restore, then removed.
- Docker Desktop on this PC. No MongoDB tools need installing: the commands run from the `mongo` image.

Replace the placeholders below: `DEVHOST` (where dev MongoDB listens as seen from a container, usually `host.docker.internal:27017`), `DEVUSER`/`DEVPASS`, `ATLASHOST` (the `cluster0.xxxxx.mongodb.net` part), `ATLASUSER`/`ATLASPASS`.

## 1. Dump the dev database

Work in a directory that is not inside the repository and is not synced (not OneDrive or Dropbox):

```bash
mkdir -p ~/notary-migration && cd ~/notary-migration
sudo docker run --rm mongo:7 mongodump \
  --uri "mongodb://DEVUSER:DEVPASS@DEVHOST/?authSource=admin" \
  --db notary --archive --gzip > notary-dev.archive
ls -l notary-dev.archive
```

The archive goes through stdout so your shell creates the file. Mounting a folder into the container fails with `permission denied`, because the container's `mongodb` user cannot write to your directory. Errors print to the terminal, so a tiny or empty archive means the dump failed.

Check what is in the dump:

```bash
sudo docker run --rm -i mongo:7 mongorestore --archive --gzip --dryRun --verbose < notary-dev.archive 2>&1 | grep -E "restoring|documents"
```

## 2. Dry run into a scratch database

Restore under a different database name so nothing real is touched. `--nsFrom/--nsTo` renames `notary` to `notary_dryrun`:

```bash
sudo docker run --rm -i mongo:7 mongorestore \
  --uri "mongodb+srv://ATLASUSER:ATLASPASS@ATLASHOST/?authSource=admin" \
  --archive --gzip \
  --nsFrom 'notary.*' --nsTo 'notary_dryrun.*' < notary-dev.archive
```

The Atlas user needs `readWrite` on `notary_dryrun` for this.

Then prove it works: run a local Notary (Visual Studio or `dotnet run`) with `NOTARY_DB_HOST=mongodb+srv://ATLASHOST`, `NOTARY_DB_NAME=notary_dryrun`, the Atlas credentials and the **production** `NOTARY_APP_KEY`, and:

1. Read the startup log: it must say `Private key migration: 0 migrated, ... 0 failed`. A non-zero failed count means the app key is wrong.
2. Open the intermediate CA and issue a throwaway leaf certificate from it. This proves the stored CA key decrypts and signs.
3. Fetch the CRL from your **local** Notary and check it. The URL shown in the certificate points at `http://notary.unionsquared.lan`, which does not serve this Notary yet, so keep the path from the certificate (`/api/crl/<id>`) and use the debug address in front of it. With the `http` launch profile that is `http://localhost:15176` (`https://localhost:17244` for the `https` profile; check `src/web/Properties/launchSettings.json` if you changed it):
   ```bash
   curl -s http://localhost:15176/api/crl/<id> -o dryrun.crl
   openssl crl -inform DER -in dryrun.crl -noout -text
   ```
   Or open the same address in a browser to download the file, then run the `openssl` command on it. The output should name the intermediate as issuer, show a `Next Update` in the future, and (if you revoked the throwaway certificate) list its serial. If the browser or `curl` rejects the `https` address, use the `http` one.
4. Drop the scratch database in Atlas (Browse Collections > `notary_dryrun` > Drop Database).

Record the result:

| Check | Result | Date |
| --- | --- | --- |
| Restore finished, document counts match the dump | | |
| Startup log shows 0 failed | | |
| Throwaway leaf issued from the intermediate | | |
| CRL downloads and parses | | |
| Scratch database dropped | | |

## 3. Restore into production

Only after the dry run passes. If `notary` already has data in Atlas (a failed earlier attempt), take a backup first (Atlas > Backup, or run the dump command against it) and add `--drop` to replace the collections:

```bash
sudo docker run --rm -i mongo:7 mongorestore \
  --uri "mongodb+srv://ATLASUSER:ATLASPASS@ATLASHOST/?authSource=admin" \
  --archive --gzip --nsInclude 'notary.*' < notary-dev.archive
```

## 4. Point production at it

In `/opt/notary/.env` on the Pi: `NOTARY_DB_HOST`, `NOTARY_DB_NAME=notary`, the Atlas credentials, the same `NOTARY_APP_KEY` as dev and `NOTARY_CRL_ENDPOINT=http://notary.unionsquared.lan`. Then `docker compose up -d`.

## 5. Clean up

- Delete the dump: `shred -u ~/notary-migration/notary-dev.archive` (on Windows, delete it and empty the recycle bin).
- Remove this PC's IP from the Atlas allow-list.
- Do not put the archive in the repository, a chat, or a shared drive. If you must keep one, keep it encrypted (for example `age` or an encrypted archive) with the password stored separately.

## 6. Verify production

1. `docker compose logs notary-web | grep "Private key migration"` shows `0 failed`.
2. Sign in at `https://notary.unionsquared.lan`; the root, intermediate and leaf appear.
3. Issue a test certificate from the intermediate, check it, then revoke it and confirm the CRL at `http://notary.unionsquared.lan/api/crl/<issuer>` lists it.

## Rollback

Production is unchanged until step 4, so abandoning before it costs nothing: drop the scratch database and delete the dump. After step 4, point `.env` back at the previous database (or stop the stack); the dev database is untouched by the whole procedure and remains the source of truth until you are satisfied.

## Gaps

There is no import/export tool in the app. If this procedure proves unsafe or incomplete, open an issue for one rather than working around it.
