# Test Report: ec-key-load-tests

Run: run-role-based-authorization-398ceb-002

## Results
- `dotnet test ./src/test/`: 396 passed, 0 failed, 0 skipped (13 new tests; was 383)
- Coverage: not collected

## Acceptance Criteria Validation
- [x] P-256/384/521: loaded public key SPKI equals that of a self-signed certificate built in-test, and equals `GetPublicKey`
- [x] Validation helper rejects point at infinity and off-curve point (also null); accepts valid points
- [x] Existing round-trip sign/verify tests cover all three curves; no duplicate added
- [x] Full suite passes
