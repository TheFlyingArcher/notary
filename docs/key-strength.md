# Mixed RSA/EC chains and the key strength rule

A certificate authority can use a different key type than the CA above it. An RSA root can sign Elliptic Curve (EC)
intermediates, and an EC CA can issue RSA certificates. Such chains are valid X.509 and common in public PKI.

To keep the root's security level as a floor, Notary enforces one rule when it issues something new:

> A CA or certificate may never be weaker than the CA that issues it.

Strength can stay the same or increase going down a chain. A CA created with the strongest settings therefore passes
the strongest settings to everything below it.

## How strength is compared

RSA and EC keys are compared by security bits from NIST SP 800-57, not by key size:

| Key | Security bits |
| --- | --- |
| RSA below 2048 bits | 80 |
| RSA 2048 to 3071 bits | 112 |
| RSA 3072 to 7679 bits (3072 and 4096 are the offered sizes) | 128 |
| P-256 | 128 |
| RSA 7680 to 15359 bits | 192 |
| P-384 | 192 |
| RSA 15360 bits and above | 256 |
| P-521 | 256 |

A key size between two rows takes the row below it. Equal strength is allowed, whatever the key types, so:

- RSA 3072 and RSA 4096 are the same level. A 3072-bit key can sit under a 4096-bit CA. The pages label this as
  "Same security level" rather than a smaller key.
- RSA 3072 and P-256 can issue to each other.
- RSA 2048 (112 bits) is weaker than P-256 (128 bits), so a P-256 CA cannot issue an RSA 2048 certificate, while an
  RSA 2048 CA can issue a P-256 one.
- A P-384 CA (192 bits) cannot issue any RSA certificate Notary offers, because RSA 4096 is 128 bits. Under a P-384
  CA only P-384 and P-521 are available.

## Where it is enforced

The rule is checked on the server, so the API and the pages follow it equally. A violation is rejected with an
invalid-request result, for example "A 112-bit key cannot be issued under a 128-bit CA; it must be at least as strong
as its issuer.", and nothing is created: no key, certificate or CA record.

- **Creating a CA under a parent** (`CertificateAuthorityService.CreateAsync`). The child chooses its own key
  algorithm, curve and length. It no longer copies its parent's.
- **Issuing a certificate under a CA** (`CertificateService.IssueCertificateAsync`). This covers leaf certificates
  and the certificate of a CA. The issuer's strength is read from its actual key.

Self-signed certificates and root CAs have no issuer and are not compared with anything. Only the direct issuer is
compared.

## Signatures follow the issuer's key

The signature algorithm comes from the issuer's key, not the subject's: P-256 signs with SHA-256, P-384 with SHA-384
and P-521 with SHA-512; RSA up to 2048 bits signs with SHA-256, up to 3072 with SHA-384 and larger with SHA-512. An RSA
certificate under an EC CA is therefore signed with ECDSA, and the reverse.

## The pages

**Create Certificate Authority** (under a parent) and **Create Certificate** default the key to the issuing CA's own
key. The key section can be opened to choose another type or size, and it offers only options at least as strong as
the issuer:

- Each option shows its security level, for example "RSA 3072 bit - 128-bit security".
- A neutral label says how it compares with the issuing CA: "Same as issuing CA", "Same security level" or
  "Stronger".
- RSA or Elliptic Curve is shown only if at least one size or curve of that type qualifies.
- Switching key type picks the weakest allowed option of the new type.

A root CA or a self-signed certificate has no issuer, so every option is offered.

## Existing chains

The rule applies to new issuance only. Chains created before it, which may already have a CA weaker than its issuer,
keep loading, validating and publishing CRLs. Creating something new under such a CA is compared with that CA's own
key, not with the CAs above it.
