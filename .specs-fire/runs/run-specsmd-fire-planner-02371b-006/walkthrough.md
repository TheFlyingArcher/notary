---
run: run-specsmd-fire-planner-02371b-006
work_item: leaf-key-strength-floor
intent: mixed-chain-key-strength
generated: 2026-10-26T00:00:00Z
mode: confirm
---

# Implementation Walkthrough: Leaf key strength floor

## Summary

`IssueCertificateAsync` now rejects a certificate whose key is weaker than the key of the CA that issues it. The check runs before a key is generated or anything is saved, and it applies to leaves and CA certificates alike.

## Files Changed

### Created

| File | Purpose |
|------|---------|
| `src/library/service/KeySpecs.cs` | Reads an EC curve or RSA size from a real BouncyCastle key as a `KeySpec` |
| `src/test/LeafKeyStrengthTest.cs` | Tests for the floor and for `KeySpecs` |

### Modified

| File | Changes |
|------|---------|
| `src/library/service/CertificateService.cs` | After the issuer's key pair is loaded: `NotFound` if missing, then `KeyStrength.CheckNotWeaker(issuer, request)` |
| `src/test/InMemoryFakes.cs` | `FakeKeyService.Count` |
| `src/test/SignatureAlgorithmTest.cs` | The RSA-under-EC mixed pairing test now uses RSA-3072 under P-256 |

## Key Implementation Details

1. The issuer's strength comes from its actual key (field size 256/384/521 or RSA modulus bits), so it works without a CA record and for legacy keys.
2. The subject's strength comes from the request. A request with no curve or size fails as `Invalid` before a key is made.
3. Self-signed certificates have no parent and skip the check.

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Issuer source | The real key pair | Authoritative; no dependency on stored metadata |
| Where | Before key generation | A rejected request leaves no orphan key |
| CA certificates | Checked too | They go through the same method; message matches the CA service |

## How to Verify

```bash
dotnet test ./src/test/ --filter "FullyQualifiedName~LeafKeyStrengthTest"
```

All 539 tests pass.
