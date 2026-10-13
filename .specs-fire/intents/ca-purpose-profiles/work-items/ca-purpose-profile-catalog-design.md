---
work_item: ca-purpose-profile-catalog
intent: ca-purpose-profiles
created: 2026-10-13T00:00:00Z
mode: validate
checkpoint_1: approved
---

# Design: CA purpose profile catalog

## Summary

A pure library of CA purposes and their profiles, with no UI and no persistence. For each purpose it defines what the CA certificate carries and what the certificates it issues may carry. It also supplies a check that a child CA stays within its parent's profile.

## Scope

**In Scope:**
- `CaPurpose` enum and `CaProfile` positional record
- Catalog of the seven profiles, with a null-to-Custom default
- Path length rule
- Child-within-parent check returning `Result`
- Unit tests

**Out of Scope:**
- Persisting the purpose (ca-purpose-persistence)
- Building CA certificates from profiles (ca-cert-from-profile)
- Enforcing profiles on leaf issuance (leaf-profile-enforcement)
- Any UI

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| CA key usage | `keyCertSign` + `cRLSign` only, for every purpose and Root | Today's CAs also carry digitalSignature, keyEncipherment and dataEncipherment, which a CA does not need. Fewer usages means less to abuse. |
| EKU on CA certificate | Purpose's EKUs; none on Root | Chosen by the user. Windows enforces EKU down the chain; OpenSSL and Java mostly do not, so Notary also enforces server-side. |
| Root issues | CA certificates only. Root NEVER issues leaf certificates. | Confirmed by the user. Roots only sign intermediate CAs. Existing CAs are Custom, so unaffected. |
| Path length | None for Root and self-signed purpose CAs; 0 for a purpose CA that has a parent | An intermediate then issues leaves only, while a self-signed purpose CA can still have same-purpose children. |
| Child CA rule | The child's EKUs must be a subset of its parent's. Root can never be a child. | Matches strict chain validation and prevents widening. |
| Custom under a restricted parent | Must list explicit EKUs inside the parent's set | An unrestricted Custom CA would otherwise escape its parent. |
| Missing or null purpose | Resolves to Custom (unrestricted) | Existing CAs keep working. |
| TLS/SSL | Server Auth only | Matches "Web server, HTTPS". mTLS clients use an IoT, 802.1x or Custom CA. |
| IoT and 802.1x | Identical usages | Differ only in purpose and trust separation. |

## Approved Profiles

| Purpose | CA EKUs | Leaf key usages | Leaf EKUs |
|---|---|---|---|
| Root | none | n/a (issues CAs only) | n/a |
| TLS/SSL | Server Auth | Digital Signature, Key Encipherment | Server Auth |
| User login | Client Auth, Smart Card Login | Digital Signature, Key Encipherment | Client Auth, Smart Card Login |
| S/MIME | E-mail Protection | Digital Signature, Non Repudiation, Key Encipherment | E-mail Protection |
| IoT devices | Client Auth, Server Auth | Digital Signature, Key Encipherment | Client Auth, Server Auth |
| 802.1x/RADIUS | Client Auth, Server Auth | Digital Signature, Key Encipherment | Client Auth, Server Auth |
| Custom (discouraged) | user-chosen | user-chosen | user-chosen |

## Data Models Affected

### Creates
- **CaPurpose**: Root, TlsSsl, UserLogin, SMime, IoT, Dot1xRadius, Custom — purpose of a CA
- **CaProfile**: Purpose, DisplayName, Description, IsDiscouraged, CaKeyUsages, CaExtendedKeyUsages, LeafKeyUsages, LeafExtendedKeyUsages, IssuesLeafCertificates, IssuesCaCertificates — immutable positional record

## Technical Approach

### Architecture

```
CaPurpose (enum)
   |
CaProfile (positional record)
   |
CaProfileCatalog (static)
   Get(purpose?)                      -> profile (null -> Custom)
   GetAll()                           -> the seven, for the UI
   GetPathLength(purpose, hasParent)  -> int?
   CheckChild(parent, child)          -> Result
```

Namespace `Notary.Profiles` in the core library, using the existing `CertificateKeyUsage` enum and EKU OID strings. Lists are immutable. EKU OIDs are named constants.

## Affected Files

| File | Action | Purpose |
|------|--------|---------|
| `src/library/core/Profiles/CaPurpose.cs` | create | Purpose enum |
| `src/library/core/Profiles/CaProfile.cs` | create | Profile record |
| `src/library/core/Profiles/CaProfileCatalog.cs` | create | Catalog, path length, child check |
| `src/test/CaProfileCatalogTest.cs` | create | Unit tests |

## Security Considerations

- **Least privilege on CA usages**: CA certificates carry only `keyCertSign` and `cRLSign`.
- **No widening down the chain**: a child can only narrow its parent's EKUs, and Custom must name its EKUs explicitly under a restricted parent.
- **Roots do not issue leaves**: enforced in leaf-profile-enforcement using `IssuesLeafCertificates`.

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Validators differ on EKU-in-CA enforcement | Medium | Notary enforces server-side regardless. Root carries no EKU. |
| Smart Card Login profile too loose for Windows logon | Medium | Draft is Client Auth + Smart Card Login. Review on a real Windows test before relying on it. |
| Profiles become wrong over time | Low | One catalog with tests, so changes are one-line edits. |
| Rejecting leaves from Root surprises users | Low | Selector UI explains it; existing CAs are Custom. |

## Implementation Checklist

- [ ] Add `CaPurpose` and `CaProfile`
- [ ] Add EKU OID constants
- [ ] Add `CaProfileCatalog` with the seven profiles and the null-to-Custom default
- [ ] Add `GetPathLength`
- [ ] Add `CheckChild` (subset rule, Root never a child, Custom needs explicit EKUs under a restricted parent)
- [ ] Unit tests per profile and per rule

---
*Generated by specs.md - fabriqa.ai FIRE Flow | Checkpoint 1 approved: 2026-10-13T00:00:00Z*
