---
run: run-specsmd-fire-planner-01019e-005
work_item: certificates-page-create-buttons
intent: organize-certificates-by-ca
mode: autopilot
checkpoint: none
approved_at: n/a
---

# Implementation Plan: Create buttons on the Certificates page

## Approach

In `Certificates.razor`, replace the single Create Certificate button with two:

- **Create Certificate**: links to `/certificates/create?ca={escaped CA slug}` for the selected CA. Disabled while loading, when Self-signed is selected, or when no CA is available.
- **Create Self-Signed Certificate**: links to `/certificates/create-self-signed`.

Both stay inside the existing `AuthorizeView Roles="NotaryAdmin,NotaryWriter"`, and render disabled for other users. The CA slug is URL-escaped.

## Files to Create

| File | Purpose |
|------|---------|
| (none) | |

## Files to Modify

| File | Changes |
|------|---------|
| `src/web/Pages/Certificates.razor` | Two create buttons wired to the selection |
| `src/web/Pages/Certificates.razor.cs` | `CreateCertificateHref` helper |
