---
id: hierarchical-ca-table
title: Hierarchical CA table
intent: hierarchical-ca-ordering
complexity: medium
mode: confirm
status: completed
depends_on:
  - ca-hierarchy-ordering
  - create-root-ca-form
  - create-intermediate-ca-under-parent
created: 2026-10-14T00:00:00Z
run_id: run-specs-md-fire-planner-fad6e4-001
completed_at: 2026-10-14T23:32:46.906Z
---

# Work Item: Hierarchical CA table

## Description

/ca renders the ordered rows in the existing MudTable with indentation, purpose icons, Create Root CA and per-row + buttons.

## Acceptance Criteria

- [ ] Rows are depth-first with the Name indented by depth
- [ ] Button reads Create Root CA and links to the root form; empty-state text updated
- [ ] Round + button on each row that can issue CAs, linking to ?parent=; hidden or disabled otherwise
- [ ] Intermediate rows show the purpose icon (http, assignment_ind, email, devices, wifi_password, tune); legacy CAs with no purpose use tune
- [ ] Existing Details and Download buttons remain on every row
- [ ] Verified in the running UI

## Technical Notes

src/web/Pages/CertificateAuthorities.razor(.cs). Keep tabular format; no MudTreeView.

## Dependencies

- ca-hierarchy-ordering
- create-root-ca-form
- create-intermediate-ca-under-parent
