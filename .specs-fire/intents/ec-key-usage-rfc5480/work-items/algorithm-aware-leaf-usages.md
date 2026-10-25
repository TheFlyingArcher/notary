---
id: algorithm-aware-leaf-usages
title: Algorithm-aware profile leaf usages and EC rejection
intent: ec-key-usage-rfc5480
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-24T00:00:00Z
run_id: run-github-issue-51-4dca63-003
completed_at: 2026-10-25T00:09:33.563Z
---

# Work Item: Algorithm-aware profile leaf usages and EC rejection

## Description

Make a profile's permitted leaf key usages depend on the subject key algorithm, and reject keyEncipherment/dataEncipherment on EC subject keys at issuance.

## Acceptance Criteria

- [ ] `CaProfile` exposes the leaf key usages for a given key algorithm; RSA returns today's sets
- [ ] EC: TLS/SSL, User login, IoT and 802.1x allow DigitalSignature only; S/MIME allows DigitalSignature, NonRepudiation, KeyAgreement
- [ ] `CaProfileCatalog.CheckLeaf` takes the subject key algorithm and fails with an Invalid result naming the offending usages for EC + KeyEncipherment/DataEncipherment, including Custom and unrestricted issuers
- [ ] `CertificateService.IssueCertificateAsync` passes the request's key algorithm and returns the failure; no certificate is saved
- [ ] `CreateCertificate` form applies the algorithm-specific locked usages and shows them in its description

## Technical Notes

`src/library/core/Profiles/CaProfile.cs`, `CaProfileCatalog.cs` (`CheckLeaf`, leaf sets), `src/library/service/CertificateService.cs` (~line 99-105), `src/web/Pages/CreateCertificate.razor.cs` (~lines 179, 200). Keep the RSA behaviour byte-for-byte. Existing `CheckLeaf` callers and tests need the new parameter.

## Dependencies

(none)
