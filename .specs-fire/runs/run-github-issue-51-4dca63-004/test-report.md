# Test Report: run-github-issue-51-4dca63-004

## Work Item: pkcs12-modern-export-with-chain
- `Pkcs12ExportBuilder` (.NET `Pkcs12Builder`) and `CertificateChainResolver`; `RequestCertificateAsync` uses them.
- Verified (EC and RSA): MAC verifies, wrong password fails; leaf, intermediate and root present in order; private key matches the leaf; PBES2 AES-256-CBC, PBKDF2-HMAC-SHA256 and at least 600,000 iterations asserted from the ASN.1 for both key and certificates; SHA-256 MAC; self-signed exports alone; missing issuer fails clearly; missing password refused; loop and depth guards.
- OpenSSL 3.5 opens the file without `-legacy` and reports AES-256-CBC, hmacWithSHA256, 600000 iterations, MAC sha256. A test caught an initial malformed certificate bag (missing OCTET STRING wrap), fixed.

## Work Item: pkcs7-chain-export
- `Pkcs7` returns a certs-only SignedData (no signers, no content) with the leaf and chain; dialog offers PKCS #7 (chain) and names it `.p7b`. `openssl pkcs7 -print_certs` reads 3 certificates. Certificate order inside a `.p7b` is not guaranteed.

## Work Item: certificate-export-tests
- 19 new tests in `CertificateExportTest` (none skipped; OpenSSL was available).
- Full suite and coverage: see below.

### Not verified here
Blazor dialog rendering, and import on Windows and macOS (UAT).

- Full suite: 482 passed, 0 failed, 0 skipped. Coverage 72.91% line (threshold 65%).
