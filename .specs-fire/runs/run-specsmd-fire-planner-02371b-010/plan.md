---
run: run-specsmd-fire-planner-02371b-010
work_item: key-strength-tests-docs
intent: mixed-chain-key-strength
mode: confirm
checkpoint: plan
approved_at: 2026-10-26T00:00:00Z
---

# Implementation Plan: Key strength tests and docs

## Approach

Close the intent with tests that build real mixed chains through the real services, and a short doc.

**Tests** (new `src/test/MixedChainTest.cs`, using the in-memory fakes with `CertificateAuthorityService` and `CertificateService` wired together):
- **Mixed chains are created and verify.** An RSA-3072 root, an EC P-256 intermediate (equal strength, different type), and an EC P-384 leaf under that intermediate would fail the floor, so the chain is: RSA-3072 root, EC P-256 intermediate, RSA-3072 leaf and EC P-256 leaf. Each certificate verifies against its issuer's public key, and the signature algorithm follows the issuer (ecdsa-with-SHA256 under the EC intermediate, sha256WithRSA under the RSA root). Also an EC P-384 root with an EC P-384 intermediate and a P-521 leaf (higher).
- **Each issuer/subject type combination** for both CA creation and leaf issuance: RSA→RSA, RSA→EC, EC→RSA, EC→EC, each accepted at equal or higher strength and rejected one step below, using only fast sizes where possible (RSA 2048 and 3072, P-256/384/521).
- **Equal-strength boundary:** RSA-3072 CA with a P-256 leaf and the reverse accepted; RSA-2048 under RSA-3072 and P-256 under P-384 rejected. The RSA-3072 / RSA-4096 same-level case is covered by the existing `KeyStrengthTest` and `KeyOptionsTest` (no 4096-bit key is generated).
- **Rejections leave nothing behind:** a rejected CA is not saved and issues no certificate.
- **Legacy chains still work:** a CA record that already sits below its own issuer keeps loading; creating a child under it compares only to that direct parent and succeeds at the parent's strength (added to `CertificateAuthorityServiceTest`, where a grandparent CA is stronger than the parent).
- `LeafKeyStrengthTest` and `CertificateAuthorityServiceTest` already cover single decisions; this file adds the end-to-end chains and avoids repeating them.

**Docs:**
- New `docs/key-strength.md`: what mixed chains are, the no-weaker-than-issuer rule, the security-bits table (EC curves; RSA 2048 = 112, 3072 and 4096 = 128), why RSA-3072 and RSA-4096 count as equal and can issue to each other, that signatures follow the issuer's key (#46), where it is enforced (CA creation and certificate issuance), the error users see, how the Create CA and Create Certificate pages filter and label options, and that it applies to new issuance only (existing chains keep loading and validating; only the direct issuer is compared).
- README gets one line linking to it.

## Files to Create

| File | Purpose |
|------|---------|
| `src/test/MixedChainTest.cs` | End-to-end mixed chains and the issuer/subject matrix |
| `docs/key-strength.md` | Describes the rule and the pickers |

## Files to Modify

| File | Changes |
|------|---------|
| `src/test/CertificateAuthorityServiceTest.cs` | Legacy case: a parent below its own issuer, child compared to the direct parent only |
| `README.md` | Link to the new doc |

## Tests

| Test File | Coverage |
|-----------|----------|
| `src/test/MixedChainTest.cs` | Chains RSA→EC→RSA/EC and EC→EC with signature verification and issuer-driven signature algorithms; matrix of accepted and rejected pairs for CAs and leaves; equal-strength boundaries; rejected CAs leave nothing |
| `src/test/CertificateAuthorityServiceTest.cs` | Direct-issuer-only comparison with a stronger grandparent |

## Technical Details

- RSA keys are generated at 2048 and 3072 only, in a few cases, to keep the suite fast; larger sizes are exercised through the model's unit tests.
- No production code changes.

---
*Plan approved at checkpoint. Execution follows.*
