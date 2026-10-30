# Test Report: Issued certificates table

Run: run-specsmd-fire-planner-02371b-017

- Full suite: 677 passed, 0 failed. Web project builds with 0 errors.
- New: `CertificateStatusTest` (7 cases: valid, 30-day boundary, expiring, expired, revoked, expired+revoked, expiring+revoked) and `IssuedCertificateFilterTest` (3 cases: root keeps child CAs, non-root leaf only, empty).

## Acceptance Criteria Validation

- [x] Table lists the CA's directly issued certificates via `GetCertificatesByCaAsync`
- [x] Columns: Name, Common Name, Expiry, Status, actions
- [x] View links to `certificate?slug=`; Download opens the existing dialog (disabled when revoked)
- [x] Empty-state message
- [x] Not shown on leaf certificate pages
- [x] Status logic shared (`CertificateStatusCalculator`, also drives the Info panel's Expired/Expiring) and unit tested
- [x] Root CAs list child CAs; other CAs list leaf certificates only (approved edit)
- [x] No new service methods; reuses the `CertificateList` authorization
- [ ] Visual Studio UAT — pending the user
