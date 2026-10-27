# Notary

## Overview

This is a PKI application for my Raspberry Pi server farm. It has an API component and a web component written in ASP.NET Blazor.

## Security notice

Releases v1.1.37 to v1.1.45 stored private keys unencrypted in the database. The current code encrypts them and migrates existing keys at startup. If your database or its backups could have been exposed, rotate your CA keys. See [Private key storage and NOTARY_APP_KEY](docs/private-key-storage.md) for what to do.

## Configuration

`NOTARY_APP_KEY` is required: a secret of at least 16 characters that protects the stored private keys. The application will not start without it, and changing it later makes existing keys unreadable. Details are in [docs/private-key-storage.md](docs/private-key-storage.md).

## Key types

A CA can use a different key type than its parent, so an RSA root can have EC intermediates. Notary never issues a CA or certificate weaker than its issuer. See [Mixed RSA/EC chains and the key strength rule](docs/key-strength.md).
