---
id: ec-key-usage-rfc5480
title: No keyEncipherment/dataEncipherment on EC certificates
status: completed
created: 2026-10-24T00:00:00Z
completed_at: 2026-10-25T00:09:34.147Z
---

# Intent: No keyEncipherment/dataEncipherment on EC certificates

## Goal

Make every certificate whose subject key is `id-ecPublicKey` free of the `keyEncipherment` and `dataEncipherment` key usage bits, as RFC 5480 section 3 requires, and give EC S/MIME certificates `keyAgreement` instead. Source: GitHub issue #45.

## Users

Anyone issuing EC certificates from Notary; first the TLS leaf for NGINX on the Raspberry Pi (#50), which was issued with Digital Signature + Key Encipherment on a P-384 key.

## Problem

Profile leaf usages (`CaProfileCatalog`) are fixed regardless of key algorithm, `CheckLeaf` and `IssueCertificateAsync` never look at the algorithm, the legacy CA path adds `KeyEncipherment`/`DataEncipherment` to CA certificates, and the CA form offers both bits for EC keys. Strict validators and PKI linters reject the result.

## Success Criteria

- Profile leaf key usages depend on the subject key algorithm: TLS/User login/IoT/802.1x EC leaves get DigitalSignature only; EC S/MIME gets DigitalSignature, NonRepudiation, KeyAgreement; RSA is unchanged
- `CheckLeaf` / `IssueCertificateAsync` return a `Result` failure when an EC subject key is combined with KeyEncipherment or DataEncipherment, for every profile including Custom and unrestricted CAs
- The legacy CA path no longer adds KeyEncipherment/DataEncipherment (all key types; DigitalSignature stays)
- The CA and certificate forms hide or never apply the two bits for EC keys
- Tests cover each profile with an EC key (and RSA unchanged)

## Constraints

- Allman braces, NUnit 4 + Moq in `src/test/`, existing `Result` pattern
- Make profiles algorithm-aware rather than special-casing at issuance
- Already-issued certificates are not changed; they must be re-issued
- Out of scope: #64/#46 (merged), other key-usage policy changes

## Notes

Scope confirmed with the user: legacy CA path drops both bits for all key types; EC S/MIME permits DigitalSignature, NonRepudiation and KeyAgreement.
