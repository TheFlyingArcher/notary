---
id: ca-detail-layout
title: CA detail page layout, iteration 2
status: completed
created: 2026-10-30T00:00:00Z
completed_at: 2026-10-30T23:14:45.823Z
---

# Intent: CA detail page layout, iteration 2

## Goal

Cut the dead space on the CA detail page so the panels feel balanced instead of some being narrow and some full-width. Source: GitHub issue #89 (second iteration of #86; more iterations expected).

## Users

Notary admins, writers and users who open a CA detail page.

## Problem

Info and CRL are half-width panels in separate rows, leaving empty space beside each. The Issuer Tree and Issued Certificates fill the full width, so the page looks disjointed.

## Success Criteria

CA detail pages only; leaf certificate pages are unchanged.

- Info and CRL sit side by side in one row
- On non-root CAs the Issuer Tree is a narrow left column (about 1/3) and Issued Certificates fills the right (about 2/3)
- On root CAs there is no tree, so Issued Certificates is full width
- Subject and Issuer stay side by side; Subject Alternative Names keeps its current layout
- Below MudBlazor's tablet breakpoint the side-by-side panels stack full width
- Nothing is added or removed, and the table's View and Download keep working

## Constraints

- Blazor Server with MudBlazor, mainly `src/web/Pages/CertificateDetail.razor`
- The page is shared with leaf certificates, so layout changes sit behind `IsCaCertificate`
- Layout-only: UAT in Visual Studio is the main check
- Keep it small; the issue expects further iterations
- Allman braces, NUnit 4 + Moq

## Out of Scope

- Leaf certificate pages
- New panels or data
- Changes to the Subject and Issuer panels
