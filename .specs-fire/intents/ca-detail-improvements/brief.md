---
id: ca-detail-improvements
title: Improve the CA detail page
status: completed
created: 2026-10-29T00:00:00Z
completed_at: 2026-10-30T00:45:20.287Z
---

# Intent: Improve the CA detail page

## Goal

Make the CA detail page more useful and less cluttered: clearer info panel, no meaningless issuer tree on root CAs, and a list of the certificates the CA has issued. Source: GitHub issue #86.

## Users

Notary admins and writers who manage certificate authorities.

## Problem

- The Issuer Tree is meaningless on a root CA.
- The combined "Valid: from to to" row is hard to read.
- There is no way to see what a CA has issued without going to the Certificates page and filtering.

## Success Criteria

CA detail pages only; leaf certificate pages are unchanged.

- The "Status" panel is renamed "Info"
- The "Valid" row is replaced by separate "Not Before" and "Not After" rows
- A new row shows the CA kind (Root, TLS/SSL, S/MIME, ...) from the stored CA purpose; the existing Status row (Valid/Expiring/Expired/Revoked) stays in the panel
- The Issuer Tree is hidden on root CAs
- An issued-certificates table shows on every CA (root or not), below where the tree sits, listing only certificates this CA issued directly
- Columns: Name, Common Name, Expiry, Status, and an actions column
- View opens the certificate's detail page; Download opens the existing download dialog
- Authorization follows the existing role rules

## Constraints

- Blazor Server page using MudBlazor: `src/web/Pages/CertificateDetail.razor` is shared by CAs and leaf certificates (`IsCaCertificate` flag)
- Reuse the scoped certificate listing by CA from `organize-certificates-by-ca` where possible
- Follow the service-method authorization conventions
- The issue expects iteration after first modifications; keep pieces small
- Allman braces, NUnit 4 + Moq; changes must build and run from the Visual Studio solution for UAT

## Out of Scope

- Certificates issued by sub-CAs below this CA (descendants)
- Changes to leaf certificate pages

## Notes

The issue literally says the table goes "below Issuer tree on non-root CAs"; the user chose to show it on root CAs too, since a root issues certificates as well.
