---
run: run-specsmd-fire-planner-01019e-001
work_item: certificates-by-ca-listing
intent: organize-certificates-by-ca
---

# Code Review: Scoped certificate listing by CA

No linter configured; manual review against coding standards.

## Auto-fixed

(none)

## Suggestions

(none requiring approval)

## Observations

- `GetLeafIssuingAuthoritiesAsync` looks up each CA's certificate individually (one query per CA). Fine for the expected number of CAs; batch it if CA counts grow.
- `CreateCertificate.razor.cs` still has its own leaf-issuing filter. It is replaced by the scoped-create work item, which should switch to the new service method.
- `GetCertificatesByCaAsync` takes the CA's certificate slug; the docs now say so.
