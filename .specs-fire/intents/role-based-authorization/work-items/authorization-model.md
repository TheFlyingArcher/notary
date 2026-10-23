---
id: authorization-model
title: Roles, operations and permission map
intent: role-based-authorization
complexity: medium
mode: confirm
status: completed
depends_on: []
created: 2026-10-23T00:00:00Z
run_id: run-specsmd-fire-planner-3d8043-001
completed_at: 2026-10-23T00:43:14Z
---

# Work Item: Roles, operations and permission map

## Description

Define the authorization vocabulary in `Notary.Security`.

## Acceptance Criteria

- [x] `NotaryOperation` renumbered 1-14 with unique values
- [x] `NotaryRoles` defines NotaryUser, NotaryWriter, NotaryAdmin
- [x] `NotaryRolePermissions` maps each role to operations; multiple roles give the union
- [x] `NotaryAuthorizeAttribute` (with `allowAnonymous`) and `NotaryAuthorizationException` (401) in `Notary.Security`; old `Notary.IOC` attribute removed

## Dependencies

(none)
