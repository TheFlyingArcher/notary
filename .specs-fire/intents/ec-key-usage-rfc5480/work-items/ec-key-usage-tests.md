---
id: ec-key-usage-tests
title: Tests for EC key usage rules
intent: ec-key-usage-rfc5480
complexity: medium
mode: confirm
status: completed
depends_on:
  - algorithm-aware-leaf-usages
  - ca-certificate-usages-ec
created: 2026-10-24T00:00:00Z
run_id: run-github-issue-51-4dca63-003
completed_at: 2026-10-25T00:09:34.128Z
---

# Work Item: Tests for EC key usage rules

## Description

Prove each profile and issuance path honours the EC key usage rules and RSA is unchanged.

## Acceptance Criteria

- [ ] For each profile, EC leaf usages are as specified and RSA leaf usages equal the previous sets
- [ ] `CheckLeaf` and `IssueCertificateAsync` reject EC + KeyEncipherment and EC + DataEncipherment for each profile, Custom and unrestricted issuers; nothing is saved on failure
- [ ] An issued EC certificate's keyUsage extension has no KeyEncipherment/DataEncipherment (parsed from the certificate)
- [ ] A new legacy-path CA certificate has no encipherment bits (RSA and EC); a Custom EC CA with those bits is rejected
- [ ] Whole suite passes with the CI coverage threshold

## Technical Notes

Extend the profile catalog tests, `CaPurposeIssuanceTest.cs` and the CA service tests; follow their fixtures.

## Dependencies

- algorithm-aware-leaf-usages
- ca-certificate-usages-ec
