# Test Report: run-github-issue-51-4dca63-003

## Work Item: algorithm-aware-leaf-usages
- `CaProfile.GetLeafKeyUsages(Algorithm)`, `CaProfileCatalog.CheckKeyUsagesForAlgorithm`, `CheckLeaf` with subject algorithm, enforced in `IssueCertificateAsync`; forms updated.
- Verified: EC TLS/User login/IoT/802.1x = DigitalSignature; EC S/MIME = DigitalSignature, NonRepudiation, KeyAgreement; RSA sets unchanged; EC + KeyEncipherment/DataEncipherment rejected for every profile, Custom and no-CA-record issuers; nothing saved on failure.

## Work Item: ca-certificate-usages-ec
- Legacy CA path no longer adds KeyEncipherment/DataEncipherment (RSA and EC verified by parsing the certificate). Custom EC CA with either bit is rejected before anything is saved.

## Work Item: ec-key-usage-tests
- Updated existing tests that encoded the old EC + KeyEncipherment behaviour; added 38 tests (profile sets, CheckLeaf, issuance rejection and parsed keyUsage, legacy CA certificate, Custom EC CA).
- Full suite: 463 passed, 0 failed, 0 skipped. Coverage 71.23% line (threshold 65%).

### Not verified here
Blazor form behaviour (hiding the two bits for EC) and `openssl x509 -text` on a real issued certificate (UAT).
