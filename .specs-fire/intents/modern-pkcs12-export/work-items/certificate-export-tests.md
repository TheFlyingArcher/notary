---
id: certificate-export-tests
title: Round-trip tests for certificate export
intent: modern-pkcs12-export
complexity: medium
mode: confirm
status: completed
depends_on:
  - pkcs12-modern-export-with-chain
  - pkcs7-chain-export
created: 2026-10-25T00:00:00Z
run_id: run-github-issue-51-4dca63-004
completed_at: 2026-10-25T01:26:19.369Z
---

# Work Item: Round-trip tests for certificate export

## Description

Prove PKCS #12 and PKCS #7 exports are correct, complete and use modern algorithms, for RSA and EC.

## Acceptance Criteria

- [ ] PKCS #12 round trip: parse the exported bytes with the password; the key matches the leaf's public key; the chain (leaf, intermediate, root) is present; the wrong password fails
- [ ] The exported file uses AES-256-CBC PBES2, PBKDF2-HMAC-SHA256, a SHA-256 MAC and at least 600,000 iterations (asserted from the ASN.1)
- [ ] A self-signed certificate exports alone; a missing issuer fails with a clear error
- [ ] PKCS #7 parses to a certs-only SignedData with exactly the leaf and its chain and no private key material
- [ ] An OpenSSL test runs `openssl pkcs12` and `openssl pkcs7` on the exports when `openssl` is on the path (skipped with `Assume` otherwise) and passes without `-legacy`
- [ ] Whole suite passes with the CI coverage threshold

## Technical Notes

Build certificates through the issuance fakes as in `CaPurposeIssuanceTest` (root, intermediate, leaf). Put new tests in a `CertificateExportTest` file. The CI runner (ubuntu) has OpenSSL 3, so the OpenSSL test runs there.

## Dependencies

- pkcs12-modern-export-with-chain
- pkcs7-chain-export
