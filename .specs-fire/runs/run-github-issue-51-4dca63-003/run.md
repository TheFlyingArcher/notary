---
id: run-github-issue-51-4dca63-003
scope: batch
work_items:
  - id: algorithm-aware-leaf-usages
    intent: ec-key-usage-rfc5480
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
  - id: ca-certificate-usages-ec
    intent: ec-key-usage-rfc5480
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
  - id: ec-key-usage-tests
    intent: ec-key-usage-rfc5480
    mode: confirm
    status: completed
    current_phase: review
    checkpoint_state: approved
    current_checkpoint: plan
current_item: null
status: completed
started: 2026-10-25T00:00:18.447Z
completed: 2026-10-25T00:09:34.128Z
---

# Run: run-github-issue-51-4dca63-003

## Scope
batch (3 work items)

## Work Items
1. **algorithm-aware-leaf-usages** (confirm) — completed
2. **ca-certificate-usages-ec** (confirm) — completed
3. **ec-key-usage-tests** (confirm) — completed


## Current Item
(all completed)

## Files Created
(none)

## Files Modified
- `src/library/core/Profiles/CaProfile.cs`: GetLeafKeyUsages(Algorithm)
- `src/library/core/Profiles/CaProfileCatalog.cs`: EC key usage rule; CheckLeaf takes subject algorithm
- `src/library/service/CertificateService.cs`: Enforce rule at issuance
- `src/library/service/CertificateAuthorityService.cs`: Legacy CA usages drop encipherment
- `src/web/Pages/CreateCertificate.razor.cs`: Algorithm-aware usages
- `src/web/Pages/CreateCertificateAuthority.razor.cs`: Hide encipherment for EC
- `src/test/CaProfileCatalogTest.cs`: EC tests
- `src/test/CaPurposeIssuanceTest.cs`: EC tests

## Decisions
(none)


## Summary

- Work items completed: 3
- Files created: 0
- Files modified: 8
- Tests added: 38
- Coverage: 71%
- Completed: 2026-10-25T00:09:34.128Z
