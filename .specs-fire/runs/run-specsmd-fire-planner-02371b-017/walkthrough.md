# Walkthrough: Issued certificates table

Run: run-specsmd-fire-planner-02371b-017 · Work item: ca-issued-certificates-table

CA detail pages now end with an "Issued Certificates" table (Name, Common Name, Expiry, Status, View/Download). Data comes from `CertificateService.GetCertificatesByCaAsync` using the CA's certificate slug; `IssuedCertificateFilter` keeps child CAs only for root CAs and leaf certificates otherwise. `CertificateStatusCalculator` decides Valid/Expiring/Expired/Revoked (same precedence as before) and now also feeds the Info panel's Expired/Expiring flags.

## Files

- New: `CertificateStatus.cs`, `IssuedCertificateFilter.cs` (core), `IssuedCertificateRow.cs` (web), three test files.
- Modified: `CertificateDetail.razor`, `CertificateDetail.razor.cs`, `CertificateViewModel.cs`.

## Verify

1. Root CA: no Issuer Tree; table lists its child CAs.
2. Intermediate CA: tree shown; table lists leaf certificates only, sorted by name; View opens the certificate, Download opens the dialog.
3. CA with nothing issued: "This CA has not issued any certificates."
4. Revoked certificate row: Download disabled.
5. Leaf certificate page: no table.
