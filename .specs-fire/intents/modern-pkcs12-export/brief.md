---
id: modern-pkcs12-export
title: Modern, complete PKCS
status: completed
created: 2026-10-25T00:00:00Z
completed_at: 2026-10-25T01:26:19.393Z
---

# Intent: Modern, complete PKCS #12 and PKCS #7 export

## Goal

Make certificate export produce files that current tools open and that protect private keys with current algorithms: a PKCS #12 with AES-256 protection and the whole issuing chain, plus a working PKCS #7 certificate-chain download. Source: GitHub issue #68.

## Users

Anyone downloading a certificate and key from Notary; first the operator getting the NGINX leaf key and chain onto the Raspberry Pi for #50.

## Problem

`RequestCertificateAsync` builds PKCS #12 with BouncyCastle defaults: 3DES key protection, 40-bit RC2 for the certificate, a SHA-1 MAC, 1024 iterations. OpenSSL 3 cannot open the file without `-legacy`. Only the leaf is bundled, so the chain is missing. There are no tests for PKCS #12 at all. `CertificateFormat.Pkcs7` is a `TODO` that returns null. BouncyCastle (both packages) cannot set the PKCS #12 MAC digest or iteration counts; .NET Pkcs12Builder can.

## Success Criteria

- A PKCS #12 export (built with the .NET Pkcs12Builder, since BouncyCastle cannot set the MAC digest or iteration counts) uses AES-256-CBC (PBES2, PBKDF2-HMAC-SHA256, at least 600,000 iterations) for key and certificates and a SHA-256 MAC; `openssl pkcs12` opens it without `-legacy`
- The PKCS #12 contains the leaf (with its key) and every issuing certificate up to the root
- PKCS #7 export returns a certs-only `.p7b` with the leaf and its chain, no private key; the download dialog offers it with the right file name
- Round-trip tests: key matches certificate, chain present and ordered, wrong password fails, algorithms are the modern ones, `.p7b` parses with the expected certificates; an OpenSSL-based check runs where `openssl` is available
- No legacy-compatible mode is offered

## Constraints

- Allman braces, NUnit 4 + Moq, existing `NotaryAuthorize` and Result patterns
- No change to who may download (`CertificateDownload`) or to the password rules in the dialog
- Private keys are never logged; a missing certificate still returns null as today
- Existing certificates and keys are unaffected; only the export format changes
- Out of scope: Windows/macOS import testing beyond what the user checks manually (note in UAT), PEM and DER exports

## Notes

Scope confirmed with the user: modern only (no legacy option); implement PKCS #7 as a certificate chain. A BouncyCastle.Cryptography migration was considered and dropped: neither BouncyCastle package can write a SHA-256 MAC or custom iteration counts, so it would be unneeded change.
