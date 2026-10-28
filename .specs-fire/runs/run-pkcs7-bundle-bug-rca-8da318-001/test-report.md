# Test Report: pkcs7-pem-export

## Test Results
- Passed: 583 (full `src/test` project)
- Failed: 0
- Skipped: 0 (OpenSSL is installed here, so the OpenSSL tests ran)

New tests in `CertificateExportTest`: `Pkcs7Pem_IsPemArmoredAndDecodesToTheDerExport` (RSA, EC), `Pkcs7Pem_OfASelfSignedCertificate_ContainsOnlyThatCertificate`, `Pkcs7Pem_OfAMissingCertificate_ReturnsNull`, `OpenSsl_ReadsThePkcs7PemChain_WithoutInform`. Existing DER tests, including `OpenSsl_ReadsThePkcs7Chain`, pass unchanged.

## Acceptance Criteria Validation
- [x] `Pkcs7Pem = 16` added; other values unchanged
- [x] PEM output has the `PKCS7` label and decodes to the DER export bytes
- [x] Chain gathered by `GetChainAsync`; no second builder
- [x] Missing certificate returns null; unknown format still throws (default case untouched)
- [x] PEM parses to leaf + issuers (RSA and EC), no signers; self-signed gives one certificate
- [x] `openssl pkcs7 -in f -noout` exits 0 with no `-inform`; `-print_certs` lists 3 subjects
- [x] Existing DER tests still pass

## Coverage
Not measured separately; the changed lines are exercised by the new tests.
