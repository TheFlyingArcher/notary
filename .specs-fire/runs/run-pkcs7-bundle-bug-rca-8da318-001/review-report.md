# Code Review: pkcs7-pem-export

Reviewed `CertificateFormat.cs`, `CertificateService.cs`, `CertificateExportTest.cs` (manual review by the builder; the code-review skill was not run).

- No issues requiring changes. Allman style and existing patterns followed; the PEM export is a 3-line wrapper over the DER export.
- Note: `PemEncoding.WriteString` is fully qualified inline in `CertificateService.cs` to avoid adding a using to a large file; acceptable.
- Auto-fixes applied: none.
