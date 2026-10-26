# Initial CA and TLS bootstrap

How the private CA chain and the HTTPS certificate for `notary.unionsquared.lan` are created on the dev machine and installed on the Raspberry Pi. The CA is created in a dev Notary first; moving its data to the production database is covered by the data migration step (`ca-data-migration`).

Target layout:

```
Browser --HTTPS--> host NGINX (443, deploy/nginx/notary.conf) --HTTP--> notary-web container (127.0.0.1:8080)
Clients --HTTP--> host NGINX (80, /api/crl/ only) --> notary-web   (CRL downloads)
```

## 0. Before you start

- Use a **clean dev database** and the **production `NOTARY_APP_KEY`**. Private keys are stored encrypted with that key; a CA created under a different key cannot be used after the move to production. The startup log line `Private key migration: ... 0 failed` does not prove the key is right; fetching a CRL does (see [ca-data-migration.md](ca-data-migration.md)).
- Set `NOTARY_CRL_ENDPOINT=http://notary.unionsquared.lan` **before issuing anything**. The URL is written into every certificate and cannot be changed afterwards. It is `http`, not `https`, on purpose: checking revocation over HTTPS would need the certificate being checked.
- Pick the profile for each certificate in the Notary UI; the values below are what the production chain uses.

## 1. Create the chain in Notary (dev)

| Certificate | Type | Algorithm | Lifetime | Notes |
| --- | --- | --- | --- | --- |
| Union Squared Root CA | Root CA | EC P-384 | 20 years | Self-signed, signature ecdsa-with-SHA384 |
| Union Squared Web Server CA | Intermediate CA | EC P-384 | 5 years | Signed by the root, path length 0, extended key usage serverAuth |
| notary.unionsquared.lan | Leaf (web server) | EC P-384 | 1 year | Signed by the intermediate |

Leaf subject alternative names:

- `notary.unionsquared.lan`
- `unionsquared.lan`
- `usqapp01.tailf1d37d.ts.net` (Tailscale name)
- optionally IP addresses of the Pi if you will browse by IP

EC certificates carry only the Digital Signature key usage (no key or data encipherment, RFC 5480). Notary enforces this.

## 2. Export the leaf, key and chain

1. Open the leaf, choose **Download**, format **PKCS #12**, and set a strong password. The file contains the private key, the leaf, the intermediate and the root.
2. Store the `.pfx` somewhere private. `*.pfx` is git-ignored; keep it that way.
3. Check it opens with a current OpenSSL **without** `-legacy` (the export uses AES-256-CBC, PBKDF2-HMAC-SHA256, 600,000 iterations and an HMAC-SHA256 MAC):

```bash
openssl pkcs12 -in notary-https-cert.pfx -info -nokeys | grep -E "PBES2|MAC:"
```

4. Split it into PEM files (each command prompts for the password):

```bash
openssl pkcs12 -in notary-https-cert.pfx -clcerts -nokeys -out leaf.pem
openssl pkcs12 -in notary-https-cert.pfx -cacerts -nokeys -out chain.pem
```

`chain.pem` holds the intermediate followed by the root. Build the files NGINX needs:

```bash
# leaf first, then the intermediate. The root is not sent; clients already trust it.
awk '/BEGIN CERTIFICATE/{n++} n==1' chain.pem > intermediate.pem
cat leaf.pem intermediate.pem > fullchain.pem
openssl pkcs12 -in notary-https-cert.pfx -nocerts -nodes -out privkey.pem
# root for client trust
awk '/BEGIN CERTIFICATE/{n++} n==2' chain.pem > root.pem
```

5. Verify before copying anything:

```bash
openssl verify -x509_strict -CAfile root.pem -untrusted intermediate.pem leaf.pem
# the key must match the leaf: both lines print the same hash
openssl pkey -in privkey.pem -pubout | sha256sum
openssl x509 -in leaf.pem -pubkey -noout | sha256sum
```

`privkey.pem` is an unencrypted private key. Do not leave it on the dev machine after it is installed (`shred -u privkey.pem` on Linux; delete and empty the recycle bin on Windows).

## 3. DNS

In the BIND9 zone for `unionsquared.lan` on `ns1.unionsquared.lan` (192.168.1.11), add an A record pointing at the Pi's LAN address and increase the zone serial:

```
notary    IN A    <Pi LAN IP>
```

```bash
sudo rndc reload unionsquared.lan
nslookup notary.unionsquared.lan 192.168.1.11
ping notary.unionsquared.lan
```

## 4. Auth0

In the Notary application in Auth0 set:

- Allowed Callback URLs: `https://notary.unionsquared.lan/callback`
- Allowed Logout URLs: `https://notary.unionsquared.lan/`

Keep any dev URLs you still use, comma separated.

## 5. Install on the Pi

Host NGINX is shared with other projects, so Notary adds one site and leaves the others alone.

```bash
sudo mkdir -p /etc/nginx/certs/notary
sudo cp fullchain.pem privkey.pem /etc/nginx/certs/notary/
sudo chown root:root /etc/nginx/certs/notary/*.pem
sudo chmod 644 /etc/nginx/certs/notary/fullchain.pem
sudo chmod 600 /etc/nginx/certs/notary/privkey.pem

sudo cp deploy/nginx/notary.conf /etc/nginx/sites-available/notary
sudo ln -s /etc/nginx/sites-available/notary /etc/nginx/sites-enabled/notary
sudo nginx -t && sudo systemctl reload nginx
```

In `/opt/notary/.env` set `NOTARY_CRL_ENDPOINT=http://notary.unionsquared.lan` (it must match what was set when issuing), then `docker compose up -d`.

Check:

```bash
curl -I http://notary.unionsquared.lan/          # 301 to https
curl --cacert root.pem -I https://notary.unionsquared.lan/
openssl s_client -connect notary.unionsquared.lan:443 -servername notary.unionsquared.lan -CAfile root.pem </dev/null | grep "Verify return code"
```

The last command should report `Verify return code: 0 (ok)`.

## 6. Trust the root on clients

Distribute `root.pem` (the public certificate only; never the key). Rename to `root.crt` or `root.cer` where a tool needs it.

- **Windows** (administrator): `certutil -addstore -f Root root.cer`. Or double-click, **Install Certificate**, Local Machine, **Trusted Root Certification Authorities**.
- **macOS**: `sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain root.cer`.
- **iOS / iPadOS**: AirDrop or mail the file, **Settings > Profile Downloaded > Install**, then **Settings > General > About > Certificate Trust Settings** and enable full trust for the root. Both steps are needed.
- **Linux (Debian/Ubuntu)**: `sudo cp root.crt /usr/local/share/ca-certificates/union-squared-root.crt && sudo update-ca-certificates`.
- **Firefox** keeps its own store: **Settings > Privacy & Security > View Certificates > Authorities > Import**, or enable `security.enterprise_roots.enabled`.

## 7. Renewal and expiry

| Certificate | Expires | Action |
| --- | --- | --- |
| Leaf | 2027-10-25 | Renew manually at least 30 days before |
| Intermediate | 2031-10-25 | Re-issue the intermediate and leaf before |
| Root | 2046-10-25 | Re-trust on every client before |

Track the leaf date in a calendar. Browsers reject an expired certificate with no grace period.

Renewing the leaf:

1. Issue a new leaf from the same intermediate with the same names (repeat section 1 for the leaf; section 2 for the export; section 5 for install).
2. Install the new `fullchain.pem` and `privkey.pem`, run `sudo nginx -t && sudo systemctl reload nginx`.
3. Revoke the old leaf in Notary if its key is no longer wanted.

Because there is no HSTS, an expired or replaced certificate leaves a click-through rather than a lockout.

## 8. Notes and known limits

- The CRL endpoint is plain HTTP. Only `GET` and `HEAD` on `/api/crl/` are served on port 80; everything else on port 80 redirects to HTTPS.
- The root CA's private key lives in Notary's database, encrypted with `NOTARY_APP_KEY`. Losing the application key means losing the ability to issue from the chain. Keep a copy of the key outside the Pi.

## 9. HTTPS acceptance checklist

Run after section 5 (and after the data move, if production uses the migrated CA). Tick each line.

- [ ] **Trust and names.** From a client with the root installed, `https://notary.unionsquared.lan` loads with no warning. The certificate viewer shows the chain leaf, Web Server CA, Root CA, and the SANs `notary.unionsquared.lan`, `unionsquared.lan` and `usqapp01.tailf1d37d.ts.net`.
- [ ] **Redirect.** `curl -I http://notary.unionsquared.lan/` returns `301` to `https://notary.unionsquared.lan/`.
- [ ] **Both names.** The site loads by the LAN name and by `https://usqapp01.tailf1d37d.ts.net` (from a device on the tailnet, with the root trusted there).
- [ ] **Sign-in.** Auth0 sign-in completes and returns to `https://notary.unionsquared.lan/callback`, and sign-out returns to the site.
- [ ] **Websocket.** After signing in the page stays interactive: no "Attempting to reconnect" banner or reconnect loop. In the browser developer tools Network tab the `_blazor` connection is a `101` websocket that stays open.
- [ ] **CRL.** Issue a test certificate, take the URL from its CRL Distribution Points (`http://notary.unionsquared.lan/api/crl/...`) and download it: `curl -s http://notary.unionsquared.lan/api/crl/<issuer> | openssl crl -inform DER -noout -text`. Revoke the test certificate and confirm it appears in the list.
- [ ] **Containers.** `docker compose logs notary-web` shows no errors after a restart, and a CRL download succeeds (it needs the app key to decrypt the CA key).
- [ ] **Expiry recorded.** The leaf expires 2027-10-25. Add a calendar reminder for 2027-09-25 (see section 7).
