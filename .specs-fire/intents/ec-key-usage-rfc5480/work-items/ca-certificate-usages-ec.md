---
id: ca-certificate-usages-ec
title: CA certificate usages and CA form for EC keys
intent: ec-key-usage-rfc5480
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-24T00:00:00Z
run_id: run-github-issue-51-4dca63-003
completed_at: 2026-10-25T00:09:33.843Z
---

# Work Item: CA certificate usages and CA form for EC keys

## Description

Stop CA certificates carrying the encipherment bits, and stop the CA form offering them for EC keys.

## Acceptance Criteria

- [ ] The legacy-usage path in `CertificateAuthorityService` no longer adds KeyEncipherment or DataEncipherment (DigitalSignature, KeyCertSign and CrlSign stay), for RSA and EC
- [ ] Creating a Custom CA with an EC key and KeyEncipherment/DataEncipherment fails with an Invalid result before anything is saved
- [ ] `CreateCertificateAuthority` hides KeyEncipherment and DataEncipherment (and drops any already chosen) when the selected key algorithm is EC, including a key inherited from the parent
- [ ] Existing stored CAs and certificates are unchanged

## Technical Notes

`src/library/service/CertificateAuthorityService.cs` (~lines 70-77), `src/web/Pages/CreateCertificateAuthority.razor.cs` (`OptionalKeyUsages`, ~line 177). Custom CA validation should reuse the same EC rule as the leaf check where practical.

## Dependencies

(none)
