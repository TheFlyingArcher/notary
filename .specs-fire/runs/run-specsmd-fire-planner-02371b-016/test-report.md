# Test Report: Hide the issuer tree on root CAs

Run: run-specsmd-fire-planner-02371b-016

- Full suite: 667 passed, 0 failed. Web project builds with 0 errors.
- No new unit tests: root detection is `CaDetailInfo.IsRoot`, already covered by `CaDetailInfoTest`.

## Acceptance Criteria Validation

- [x] Issuer Tree does not render when `Model.IsRootCa` is true
- [x] Non-root CAs and leaf certificates still show it (`IsRootCa` is only set for CAs)
- [x] Builds and tests pass
- [ ] Visual Studio UAT — pending the user
