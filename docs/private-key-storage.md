# Private key storage and the NOTARY_APP_KEY setting

## Security notice: unencrypted private keys in releases v1.1.37 to v1.1.45

**What happened.** From commit `f9aadd0` (22 June 2026, first released in v1.1.37) until the fix described below, Notary stored the private keys of CAs and certificates **unencrypted** in the database, even though the code and its comments said they were protected with AES-256. The cause was a library call (`Pkcs8Generator` in BouncyCastle 2.2.1) that silently ignores the password when no encryption algorithm is given. Nothing failed, so there was no sign of it at runtime. Both RSA and elliptic-curve keys were affected.

**Who is affected.** Any deployment that created keys with a release from v1.1.37 up to and including v1.1.45 (the latest at the time of writing). Keys created by earlier releases were encrypted, but with an older, weaker scheme (see below); they are upgraded by the fix.

**What was exposed.** Anyone able to read the database, or a backup or export of it, could read every affected private key and so sign as those CAs. No network attacker could do this on their own: it needs access to the stored data.

**What the fix does.**

- Keys are now stored as PKCS #8 `ENCRYPTED PRIVATE KEY` (RFC 5958), protected with PBES2 (RFC 8018): PBKDF2-HMAC-SHA256 with 600,000 iterations and a random salt per key, and AES-256-CBC with a random IV. The result can be opened with standard tools, for example `openssl pkey`.
- Writing a key without a passphrase is an error, so a plaintext key can no longer be stored by mistake.
- Loading a key that is not in this format fails instead of being used.
- At startup, existing keys are migrated automatically (see below).

**What you should do.**

1. Upgrade to a release that contains the fix and start it with a valid `NOTARY_APP_KEY`.
2. Check the startup log for the migration summary (see below).
3. **Rotate CA keys, and reissue the certificates they signed, if the database or any backup of it could have been seen by someone you do not trust** during the exposure period. Encrypting the stored keys now does not undo earlier exposure: an attacker who already copied a key still has it. A CA key that may have been exposed should be considered compromised, and every certificate it issued should be replaced and the old ones revoked.
4. Treat database backups made before the upgrade as containing unprotected (or weakly protected) private keys: restrict access to them, and delete the ones you no longer need.

If you are unsure whether your database was exposed, rotating is the safe choice for root and intermediate CAs.

## NOTARY_APP_KEY

`NOTARY_APP_KEY` is the passphrase that protects every stored private key. It is read from the environment variable of the same name.

- **Required.** The application refuses to start if it is missing, blank, or shorter than 16 characters. The startup error names the setting and never prints its value.
- **Use a long random value**, for example 32 or more random characters from a password manager. Only the length is checked, not the strength.
- **Keep it secret and keep it backed up, separately from the database.** Anyone with both the database and this value can read the keys; without it, the stored keys cannot be read by anyone, including you.
- **Do not change it** on a deployment that has keys. There is currently no command to re-encrypt keys under a new value, so changing `NOTARY_APP_KEY` makes every stored key unreadable. If you must change it, you will have to create new CAs and certificates.
- Set it the same way you set the other `NOTARY_*` variables (database, OpenID and CRL settings).

## Startup migration

On every start, before the web application serves requests, Notary looks at every stored key:

| Stored form | Where it came from | What happens |
|-------------|--------------------|--------------|
| PBES2 / PBKDF2-HMAC-SHA256 / AES-256-CBC | Current releases | Left alone |
| Plaintext `PRIVATE KEY` | v1.1.37 to v1.1.45 | Re-encrypted |
| PBES1 SHA-1 / 3DES `ENCRYPTED PRIVATE KEY` | Releases before v1.1.37 | Decrypted with `NOTARY_APP_KEY` and re-encrypted (this older scheme is weak) |

Each re-encrypted key is decrypted again and compared with the original before the database row is replaced, so a key is never replaced by something unreadable. The key itself does not change, so certificates stay valid. The step is safe to repeat: keys already in the current format are skipped.

Log lines to look for:

```text
Private key migration: 12 migrated, 0 already encrypted, 0 failed.
Private keys were re-encrypted. Database backups taken before this point may still hold the previous, weaker or unencrypted form.
Could not migrate stored private key '<slug>': ...
```

- **`failed` is more than zero**: the named row was left untouched and cannot be loaded. For an old 3DES key the usual cause is a `NOTARY_APP_KEY` different from the one the key was created with. Fix the setting and restart; the migration retries.
- Migration time grows with the number of keys: each migrated key costs two PBKDF2 runs (about 0.6 s on a fast desktop, longer on a Raspberry Pi). Keys already in the current format cost nothing, so only the first start after the upgrade is slow.
- Use the same `NOTARY_APP_KEY` that earlier releases ran with. Plaintext keys never used it, so any valid value works for them, but from the migration onward that value is required to read them.

## Rolling back

The migration is one way. After it runs, older releases cannot read the stored keys: they fail with a decryption error such as `pad block corrupted` when issuing a certificate or building a CRL, which looks like a wrong `NOTARY_APP_KEY` but is not. If you need to go back to an older release, restore a database backup taken before the upgrade (and then treat that backup as holding unprotected keys, as above) rather than pointing the older release at the migrated database.

## Downloading a certificate

The private key of a certificate authority (root or intermediate) never leaves Notary: a PKCS #12 download of a CA is refused by the service, and the download dialog does not offer it. A CA key signs everything beneath it, so a copy outside Notary would put the whole chain at risk. The CA's certificate and chain can still be downloaded in the other formats.

The private key of any other certificate only ever leaves Notary inside the password-protected PKCS #12 bundle (`.pfx`), which asks for a password that you choose and Notary does not store. The key in that file is encrypted with AES-256 (PBKDF2-HMAC-SHA256), and an export without a password is refused. Every other download format (DER, PEM, and the PKCS #7 chains) contains only public certificates and never a private key. Tests check both rules. To use the key elsewhere you open the bundle yourself, for example with `openssl pkcs12`, and from then on protecting the decrypted key is up to you.

## Not covered

- Per-key passphrases, hardware security modules or a cloud KMS.
- A command to rotate `NOTARY_APP_KEY`.
- Authenticated encryption: PBES2 with AES-CBC (the standard PKCS #8 form) has no authentication tag; a tampered stored key is detected when it fails to decrypt or parse, not by a signature.
- The encryption used when a certificate is downloaded as PKCS #12; it is a separate format with its own password.
