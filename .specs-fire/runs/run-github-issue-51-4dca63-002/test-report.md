# Test Report: run-github-issue-51-4dca63-002

## Work Item: named-curve-key-encoding

Implementation in `src/library/service/AsymmetricKeyService.cs`: named-curve generation, `ToNamedCurve` normalization on load.

### Acceptance Criteria Validation
- Generated P-256/384/521 keys: SPKI and stored PKCS#8 carry the curve OID. Verified by tests.
- Explicit-parameter stored key: loaded with the named curve, same scalar and public point. Verified.
- Unsupported explicit curve (secp256k1): rejected with "not supported". Verified.
- Stored rows not rewritten: no repository save in the load path.

## Work Item: named-curve-key-tests

### Test Results
- Added 11 tests (generated SPKI x3, stored PKCS#8 x3, explicit normalization x3, certificate SPKI, unsupported curve).
- Full suite: 425 passed, 0 failed, 0 skipped.
- Coverage: 70.79% line total (threshold 65%).
- Mutation check: with the fix reverted, all 11 new tests fail; restored, all pass.

### Not verified here
`openssl verify` against a chain issued by the running app (UAT).
