---
id: pkcs7-pem-export
title: PEM-encoded PKCS
status: completed
created: 2026-10-27T00:00:00Z
completed_at: 2026-10-28T00:52:54.456Z
---

# Intent: PEM-encoded PKCS #7 chain export

## Goal

Let users download the certificate chain as a PEM-encoded PKCS #7 (`-----BEGIN PKCS7-----`) so `openssl pkcs7 -in bundle.p7b` works with its default arguments. Keep the existing DER PKCS #7 as a separate, clearly labelled choice. Source: GitHub issue #80.

## Users

Anyone downloading a chain from Notary on Linux or macOS, where OpenSSL and most tooling default to PEM; Windows users keep DER.

## Problem

`CertificateFormat.Pkcs7` returns binary DER (`CertificateService.ExportPkcs7Async`). `openssl pkcs7` defaults to `-inform PEM`, so it fails with `unable to load PKCS7 object ... no start line`. The file is valid (the existing OpenSSL test passes with `-inform DER`), but nothing in the dialog says it is DER and there is no PEM option. See the RCA for #80.

## Success Criteria

- A new `CertificateFormat.Pkcs7Pem` export returns the same certs-only SignedData as the DER export, wrapped as PEM with the `PKCS7` label
- `openssl pkcs7 -in <file> -noout` (no `-inform`) exits 0 for the PEM download, and `-print_certs` lists the leaf and every issuing certificate
- The existing `Pkcs7` (DER) value, number and bytes are unchanged
- The download dialog shows two options, "PKCS #7 (PEM, chain)" and "PKCS #7 (DER, chain)"; both download as `<name>.p7b`; neither asks for a password
- Tests cover the PEM round trip and an OpenSSL check with no `-inform` where `openssl` is available

## Constraints

- Allman braces, NUnit 4 + Moq, existing `NotaryAuthorize` and Result patterns
- Reuse the existing chain gathering (`GetChainAsync`) and DER generation; no second PKCS #7 builder
- No change to who may download (`CertificateDownload`)
- `CertificateFormat` gets a new value (next free power of two, 16); existing values are not renumbered
- Out of scope: PEM `-----BEGIN CERTIFICATE-----` bundles, a `.p7c` extension, Windows/macOS import testing beyond manual UAT

## Notes

Decisions confirmed with the user: two separate radio options, `.p7b` for both encodings, and PEM added as a new enum value rather than changing the meaning of `Pkcs7`.
