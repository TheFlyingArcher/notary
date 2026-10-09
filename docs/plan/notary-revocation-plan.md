# Notary Certificate Revocation: Evaluation and Plan

Responds to `docs/raw/notary-raw-intent-revocation.md`.

## 1. Evaluation of the current CRL implementation

What exists: `CertificateRevokeService.GenerateCrl`, `CrlController` (`GET api/crl/{caSlug}`), a `RevocatedCertificate` record, the revoke dialog in the UI, and a CRL Distribution Point extension written into issued certificates. The BouncyCastle CRL generation itself is sound (signed, verified, `CrlNumber`, `AuthorityKeyIdentifier`, reason codes). The problems are in how it is wired.

### Defects (ordered by severity)

| # | Defect | Location | Effect |
|---|--------|----------|--------|
| 1 | CDP URL uses `parentCert.IssuingSlug` (the parent's *issuer*) instead of `parentCert.Slug` | `CertificateService.cs:65` | Certificates issued by an intermediate point at the root's CRL URL. Certificates issued directly by a root get `.../api/crl/` with an empty slug. Clients never find the right CRL. **Primary reason it "doesn't work".** |
| 2 | `GenerateCrl` includes every revoked certificate from every CA, not only those issued by `caSlug` | `CertificateRevokeService.cs:44` | CRL for CA A lists CA B's revocations, which is wrong and leaks data. Strict clients reject serials that don't belong to the issuer. |
| 3 | Controller returns `Ok(byte[])` | `CrlController.cs:19` | The default JSON formatter emits a base64 string with `application/json`. Clients need raw DER with `application/pkix-crl`. |
| 4 | Revocation flow is not atomic or idempotent. It sets `RevocationDate` and then inserts a record. Already-revoked certificates can be revoked again, creating duplicate entries. | `RevokeCertificateAsync` | Duplicate CRL entries. A partial failure leaves the certificate marked revoked but absent from the CRL. |
| 5 | `Created = DateTime.Now` (local time) is used as the revocation time | `RevokeCertificateAsync` | The CRL `revocationDate` is wrong on non-UTC hosts. |
| 6 | The CRL is rebuilt and re-signed on every anonymous request, and the CA private key is loaded each time | `GenerateCrl` | Unauthenticated DoS vector and key-exposure surface. There is no caching. |
| 7 | `caSlug` is not checked to be a CA certificate (`IsCaCertificate`), `CrlSign` key usage is not checked, and the CA's own validity and revocation status is ignored | `GenerateCrl` | Any certificate's slug can be used as a CRL issuer. |
| 8 | No Issuing Distribution Point and no AIA extension. Certificates for CAs/intermediates carry no revocation pointer. | `CertificateService` | Strict validators (and Windows) may reject or soft-fail. |
| 9 | `CertificateHold` is supported, but there is no un-hold path (`RemoveFromCrl`, code 8, is mislabelled as "whole CA removed") | `RevocationReason` | Holds are permanent in practice. |
| 10 | Reason codes: a `CaCompromized` or `KeyCompromized` revocation has no `invalidityDate`. The enum has typos and does not match RFC 5280 naming. | `RevocationReason` | Cosmetic, but `invalidityDate` matters for key compromise. |
| 11 | No authorization check in `RevokeCertificateAsync` beyond the UI. Revoking a CA does not cascade to certificates it issued. | service | Policy gap. |
| 12 | `RevocatedCertificate.SlugProperties()` is only `Thumbprint`, and nothing indexes `CertificateSlug` or the issuer | repo | Slow, unfiltered `GetAllAsync` scans as the data grows. |
| 13 | There are no tests for any revocation code | `src/test` | Nothing verifies the CRL parses or contains the right serials. |

## 2. Which method fits Notary

**Recommendation: finish CRL first (required baseline), then add OCSP as phase 2.**

- CRL is simple, cacheable, works offline, and the groundwork is already in place. Fixing the defects above gets a working revocation system quickly. For a private/internal CA with a modest certificate count the CRL stays small.
- OCSP adds real-time status and small responses, but it needs a signed-responder service, nonce/caching handling and higher availability. Doing it before CRL works would duplicate the same data-model fixes.
- Do not do OCSP stapling or CRLite; they are client/server features outside the CA's scope.
- Real clients usually want both: CRL DP and AIA/OCSP in the same certificate. Phase 2 makes that possible.

## 3. Implementation plan

### Phase 1: complete CRL (BouncyCastle, no new libraries)

1. **Fix the CDP URL** (`CertificateService.cs:65`): use `parentCert.Slug`. Validate `Configuration.CrlEndpoint` is set (absolute `http(s)` URI). Existing already-issued certificates keep the bad URL; document that they must be re-issued.
2. **Model changes**
   - `RevocatedCertificate` gains `IssuerSlug` (the CA that issued the revoked certificate, taken from `Certificate.IssuingSlug`), `RevocationDate` (UTC) and `InvalidityDate?`.
   - Add Mongo indexes: unique on `CertificateSlug`; non-unique on `IssuerSlug`.
   - `IRevocatedCertificateRepository` gets `GetByIssuerAsync(issuerSlug)`.
3. **Revoke service**
   - Reject unknown, already revoked or self-signed-root-for-CRL certificates. Return a `Result<T>`-style outcome instead of silently doing nothing.
   - Use `DateTime.UtcNow` throughout.
   - Write the revocation record first, then stamp `Certificate.RevocationDate`; on failure of the second step roll back the first.
   - Optionally cascade revocation of a CA to the certificates it issued (policy switch).
   - Add `UnrevokeAsync` for `CertificateHold` only.
4. **CRL generation**
   - Validate that `caSlug` is a CA with `CrlSign` usage and is not itself revoked or expired.
   - Filter entries by `IssuerSlug`.
   - Add `invalidityDate` and the reason-code extension per entry; add Issuing Distribution Point (`onlyContainsUserCerts` / `onlyContainsCACerts` as appropriate) if desired.
   - Replace the Unix-seconds CRL number with a persisted, monotonically increasing counter per CA (a small `CrlStateModel`: `CaSlug`, `Number`, `ThisUpdate`, `NextUpdate`, `Data`).
   - `nextUpdate` configurable (default 7 days); regenerate when a certificate is revoked or when 50% of validity has elapsed. Cache the signed DER and serve from the cache. This removes defect 6 as well.
5. **Controller**
   - `return File(crl, "application/pkix-crl")`; add `Cache-Control: max-age`, `ETag`, `Last-Modified`; return 404 for unknown/non-CA slugs; rate limit the endpoint. Accept the `.crl` suffix (`api/crl/{caSlug}.crl`).
6. **UI**: show CRL URL and next-update on the CA detail page; show revocation date/reason/invalidity date on the certificate detail page; confirm dialog before revoking CAs.
7. **Tests** (`src/test`): generate root → intermediate → leaf, revoke the leaf, parse the CRL with BouncyCastle (`X509CrlParser`) and verify signature, issuer, serial, reason, `CrlNumber` increments, entries from other CAs excluded, and CDP URL equals `{CrlEndpoint}/{issuerSlug}`. Add an interop check with `openssl verify -crl_check` or .NET `X509Chain` with `X509RevocationMode.Offline` and a custom CRL.

### Phase 2: add OCSP (RFC 6960)

1. **Library**: stay on BouncyCastle (`Org.BouncyCastle.Ocsp`: `OcspReq`, `BasicOcspRespGenerator`, `OCSPRespGenerator`). Note that `BouncyCastle.NetCore 2.2.1` is the old package id; the maintained package is `BouncyCastle.Cryptography` (2.5+) with the same namespaces. Recommend migrating as part of this work. No alternative library is needed; .NET's built-in APIs only *check* revocation, they do not build responders.
2. **Endpoint**: `POST /api/ocsp/{caSlug}` (`application/ocsp-request`) and `GET /api/ocsp/{caSlug}/{base64urlRequest}`; respond `application/ocsp-response`. Handle `good` / `revoked` (with time and reason) / `unknown`, support nonce, and `thisUpdate`/`nextUpdate` with a short cache (e.g. 1 hour).
3. **Signing key**: sign with the CA key at first; better, create a delegated OCSP-signing certificate (`id-kp-OCSPSigning` EKU, `id-pkix-ocsp-nocheck`) per CA so the CA key is not used for every query.
4. **Certificate issuance**: add the Authority Information Access extension (`id-ad-ocsp` → `{OcspEndpoint}/{issuerSlug}`; `id-ad-caIssuers` optional) in `CertificateService`; add `OcspEndpoint` to `NotaryConfiguration`.
5. **Tests**: parse the response with BouncyCastle; validate with `openssl ocsp -issuer ... -cert ... -url ...`.

### Ordering and effort
Phase 1 is roughly 1–2 days including tests and is a prerequisite for Phase 2. Phase 2 is roughly 2–3 days.

## 4. Open decisions
- Should revoking a CA cascade to everything it issued?
- Is `CertificateHold` / un-hold needed, or should it be removed from the enum?
- CRL `nextUpdate` period (suggested: 7 days)?
- Phase 2 OCSP: go ahead after CRL, or defer?
