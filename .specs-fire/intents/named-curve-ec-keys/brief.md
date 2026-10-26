---
id: named-curve-ec-keys
title: Encode EC keys with named curves
status: completed
created: 2026-10-24T00:00:00Z
completed_at: 2026-10-24T23:44:32.835Z
---

# Intent: Encode EC keys with named curves

## Goal

Make every EC key Notary generates or loads carry its curve as a named-curve OID, so issued certificates have RFC 5480-compliant SubjectPublicKeyInfo. Source: GitHub issue #64.

## Users

Anyone who issues EC certificates from Notary; first the production TLS chain for NGINX (#50).

## Problem

`AsymmetricKeyService` builds `new ECDomainParameters(SecNamedCurves.GetByOid(...))`, which drops the curve OID. `SubjectPublicKeyInfoFactory` then writes explicit curve parameters, and the stored PKCS#8 private key does the same. OpenSSL 3.5 rejects chains containing these keys (`error 94: Certificate public key has explicit ECC parameters`), and browsers and Schannel reject them too. The dev CA chain (root, intermediate, leaf, all P-384) is affected and cannot be used for HTTPS.

## Success Criteria

- Newly generated P-256, P-384 and P-521 keys produce a SubjectPublicKeyInfo with a named-curve OID
- Existing stored keys (explicit parameters in their PKCS#8 form) are matched to P-256/384/521 on load, so certificates issued from them are also named-curve; stored rows are not rewritten
- A stored key whose explicit parameters match none of the three curves fails to load with a clear error
- `openssl verify` accepts a chain issued with the fixed code (checked manually during UAT)
- Existing sign/verify, `GetPublicKey` and key-migration tests keep passing

## Constraints

- Follow existing style (Allman braces, NUnit 4 + Moq in `src/test/`)
- No schema or API change; the private scalar is unchanged, so existing keys keep working
- Certificates already issued keep explicit parameters; they must be re-issued (out of scope to automate)
- Out of scope: #45 (EC key usages), rewriting stored rows, other curves

## Notes

Scope confirmed with the user: normalize explicit parameters to a named curve on load; fail on an unknown curve; do not rewrite stored rows.
