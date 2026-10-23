# Test Report: ec-key-load-validation

Run: run-role-based-authorization-398ceb-001

## Results
- Command: `dotnet test ./src/test/`
- Passed: 383, Failed: 0, Skipped: 0
- Coverage: not collected

## Acceptance Criteria Validation
- [x] One path from `ECPrivateKeyParameters` to the key pair; no redundant catch/rethrow
- [x] Non-EC input fails with a message naming the actual type
- [x] Derived Q normalized; rejected if infinity, not on curve, or n*Q != infinity (`ValidateEcPublicPoint`)
- [x] Messages distinguish invalid key from wrong passphrase (the latter stays in `ReadEncryptedKey`); nothing logged
- [x] Valid P-256/384/521 keys load as before (existing round-trip tests pass)

## Notes
Direct tests of the rejection paths (infinity, off-curve point) are in the next work item, `ec-key-load-tests`.
