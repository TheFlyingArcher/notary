---
run: run-specsmd-fire-planner-3d8043-001
intent: role-based-authorization
generated: 2026-10-23T00:43:14Z
mode: backfilled
---

# Implementation Walkthrough: Role-based authorization on service methods

## Summary

Service methods now enforce roles themselves. Each declares its operation with `[NotaryAuthorize(NotaryOperation.X)]`; a Castle.DynamicProxy interceptor reads the user from `INotaryUserContext`, checks `NotaryRolePermissions`, and throws `NotaryAuthorizationException` (401) on denial. Backfilled from PR #55.

## Key Implementation Details

1. **Deny by default.** A method with no declared operation is denied to everyone. `allowAnonymous: true` opts out (CRL, `InitializeAsync`, startup key migration). A test fails if a service-interface method lacks an operation.
2. **Roles.** NotaryUser reads and downloads; NotaryWriter adds create and revoke certificates; NotaryAdmin has every operation. Multiple roles give the union.
3. **Async.** For `Task`-returning methods the exception is carried by the returned task.
4. **Trusted internal calls.** A Writer issuing a certificate does not need key permissions, because service-to-service calls during an authorized call are trusted.
5. **Web.** `WebNotaryUserContext` supplies roles; `Program.cs` maps the exception to HTTP 401. Non-web hosts use `NoUserContext`.
6. **Namespace move.** Interceptor and attribute moved from `Notary.IOC` to `Notary.Security[.Interceptor]`; `NotaryOperation` renumbered 1-14.

## How to Verify

```bash
dotnet test ./src/test/
```

Then sign in with each role in the web UI and confirm denied operations return 401. See `docs/authorization.md`.

## Developer Notes

Backfilled after merge: no plan, checkpoints or review report exist for this run.
