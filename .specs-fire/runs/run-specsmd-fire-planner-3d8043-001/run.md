---
id: run-specsmd-fire-planner-3d8043-001
scope: wide
work_items:
  - id: authorization-model
    intent: role-based-authorization
    mode: confirm
    status: completed
  - id: authorization-interceptor
    intent: role-based-authorization
    mode: validate
    status: completed
  - id: service-method-authorization
    intent: role-based-authorization
    mode: confirm
    status: completed
  - id: web-user-context-401
    intent: role-based-authorization
    mode: confirm
    status: completed
  - id: authorization-docs-tests
    intent: role-based-authorization
    mode: confirm
    status: completed
current_item: null
status: completed
backfilled: true
started: 2026-10-23T00:00:00Z
completed: 2026-10-23T00:43:14Z
---

# Run: run-specsmd-fire-planner-3d8043-001 (backfilled)

Reconstructed from merged PR #55 (commit 64fbddf, merge 713152a). The work was done outside FIRE; no plan, checkpoints or review report were produced at the time, so none are claimed here.

## Scope
wide (5 work items, one PR)

## Files Created
- `src/library/core/Security/{INotaryUserContext,NotaryAuthorizationException,NotaryAuthorizeAttribute,NotaryRolePermissions,NotaryRoles}.cs`
- `src/library/core/Security/Interceptor/NotaryAuthorization.cs`
- `src/library/service/NoUserContext.cs`
- `src/web/Services/WebNotaryUserContext.cs`
- `src/test/NotaryAuthorizationTest.cs`, `src/test/NotaryAuthorizationWiringTest.cs`
- `docs/authorization.md`

## Files Modified
- `src/library/core/Security/NotaryOperation.cs`, `src/library/core/Notary.csproj`
- `src/library/service/{AsymmetricKey,CertificateAuthority,CertificateRevoke,Certificate,Entity}Service.cs`, `IocRegistrations.cs`
- `src/web/Program.cs`

## Files Removed
- `src/library/core/IOC/Interceptor/NotaryAuthorization.cs`, `src/library/core/IOC/NotaryAuthorizationAttribute.cs` (moved to `Notary.Security`)

## Decisions
- Calls between services made while serving an authorized call are trusted
- No declared operation means denied to everyone
- `NotaryOperation` renumbered 1-14 (the issue's list had duplicate values)

## Summary
- Work items completed: 5
- Tests: 383 passing
- Completed: 2026-10-23T00:43:14Z
