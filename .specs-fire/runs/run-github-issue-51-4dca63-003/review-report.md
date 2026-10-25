# Review Report

Low-effort review: 1 finding (EC usage filter applied after the missing-usage check could issue a certificate with no keyUsage). Fixed by pruning before validation in CreateCertificate.OnSubmitAsync.
