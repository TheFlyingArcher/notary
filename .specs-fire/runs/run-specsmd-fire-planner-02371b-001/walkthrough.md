---
run: run-specsmd-fire-planner-02371b-001
work_item: signature-algorithm-selector
intent: match-ecdsa-hash-to-issuer-curve
generated: 2026-10-24T00:00:00Z
mode: confirm
---

# Implementation Walkthrough: Issuer signature algorithm selector

## Summary

Added `SignatureAlgorithms.ForIssuerKey`, a single shared helper that names the signature algorithm for an issuer private key. Nothing calls it yet: wiring it into certificate and CRL signing is `use-selector-in-signing`, and its tests are `signature-algorithm-tests`.

## Structure Overview

One new `internal static` class in `Notary.Service`, visible to `Notary.Test` through the existing `InternalsVisibleTo`.

## Files Changed

### Created

| File | Purpose |
|------|---------|
| `src/library/service/SignatureAlgorithms.cs` | Shared issuer-key to signature-algorithm mapping |

### Modified

(none)

## Key Implementation Details

1. **EC keys** switch on `Parameters.Curve.FieldSize`: 384 gives `SHA384WithECDSA`, 521 gives `SHA512WithECDSA`, everything else (including 256) gives `SHA256WithECDSA`.
2. **RSA keys** switch on `Modulus.BitLength`: 2048 or less gives `SHA256WithRSA`, up to 3072 gives `SHA384WithRSA`, larger gives `SHA512WithRSA`. A size between bands takes the next band's hash.
3. **Other key types and null** throw `NotaryException` naming the type, because reaching that branch is a programming error rather than a business failure.

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Helper visibility | `internal` | Only the service project and its tests need it |
| Matched EC type | `ECKeyParameters` | Covers private keys; a public key fails later in `Asn1SignatureFactory` |
| Unsupported key | Throw `NotaryException` | Programming error, not an expected failure |

## How to Verify

```bash
dotnet build ./src/library/service
dotnet test ./src/test/
```

Build is clean and all 396 existing tests pass. The mapping itself is verified by tests in the next-but-one work item.
