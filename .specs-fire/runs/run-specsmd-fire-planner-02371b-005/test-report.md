---
run: run-specsmd-fire-planner-02371b-005
work_item: child-ca-own-key-settings
intent: mixed-chain-key-strength
generated: 2026-10-26T00:00:00Z
status: passed
---

# Test Report: Child CA own key settings

## Summary

| Result | Count |
|--------|-------|
| Passed | 525 |
| Failed | 0 |
| Skipped | 0 |

12 test cases were added to `CertificateAuthorityServiceTest` (6 accepted, 4 rejected, 1 missing curve, 1 root). Two existing child tests were given explicit, valid key settings.

## Acceptance Criteria Validation

- ✅ A child CA keeps the algorithm, curve and length it was created with, and the issuance request carries them (RSA→EC, EC→RSA, EC→EC, RSA→RSA at equal or higher strength)
- ✅ An RSA child without a length is still rejected by the existing guard; an EC child without a curve is rejected as `Invalid` with nothing issued
- ✅ A child weaker than its direct parent is rejected with `ResultStatus.Invalid`, and nothing is issued or saved (P-384→P-256, P-256→RSA-2048, RSA-3072→RSA-2048, RSA-7680→P-256)
- ✅ Equal and higher strength is accepted, including across RSA and EC
- ✅ Root CAs are unaffected
- ✅ `dotnet test ./src/test/` passes (525/525)

## Tests Written

- `src/test/CertificateAuthorityServiceTest.cs` — own key settings kept, weaker rejected, missing curve, root

## Test Commands

```bash
dotnet test ./src/test/ --filter "FullyQualifiedName~CertificateAuthorityServiceTest"
```

## Notes

The fixture is a single instance, so the mocks kept calls from earlier tests and made `Times.Never` assertions fail only in the full run. `Setup` now resets both mocks.
