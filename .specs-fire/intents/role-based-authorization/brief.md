---
id: role-based-authorization
title: Role-based authorization on service methods
status: completed
created: 2026-10-23T00:00:00Z
source: GitHub issue #54, delivered in PR #55 (backfilled after merge)
completed_at: 2026-10-23T00:43:14Z
---

# Intent: Role-based authorization on service methods

> Backfilled: PR #55 was built without FIRE planning. This record reconstructs the intent, work items and run from the merged PR so the memory bank matches the code.

## Goal

Finish the Castle.DynamicProxy authorization interceptor so that every service method is checked against the caller's role as a second layer behind the page-level `[Authorize(Roles = ...)]`.

## Users

Notary operators and the people they grant `NotaryUser`, `NotaryWriter` and `NotaryAdmin` roles to.

## Problem

An earlier, unfinished interceptor and `NotaryAuthorizationAttribute` existed, but nothing enforced roles below the page. A role that got past a page could call any service operation. The issue's `NotaryOperation` list also had duplicate values.

## Success Criteria

- A service method declares its operation with `[NotaryAuthorize(NotaryOperation.X)]`; the interceptor checks the user's roles against that operation
- A denied call fails with `NotaryAuthorizationException` (401), including for async methods, and the web app returns HTTP 401
- A method with no declared operation is denied to everyone; `allowAnonymous: true` opts out (CRL, `InitializeAsync`, startup key migration)
- `NotaryUser`, `NotaryWriter` and `NotaryAdmin` map to operations in one place; multiple roles give the union
- Interceptor and attribute live in `Notary.Security[.Interceptor]`; `NotaryOperation` values are unique (1-14)
- Calls between services made while serving an authorized call are trusted
- Tests cover the role matrix, the interceptor and the Autofac wiring; behavior documented

## Constraints

- Castle.DynamicProxy interceptor registered through `IocRegistrations` on the four services
- Shared `EntityService<T>` methods overridden per service so each can declare its own operation
- Visual Studio UAT with each role

## Notes

Delivered as PR #55 (merge commit 713152a), closes #54. 383 tests passing at merge. Docs: `docs/authorization.md`.
