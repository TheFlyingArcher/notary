---
id: authorization-interceptor
title: Authorization interceptor and user context
intent: role-based-authorization
complexity: high
mode: validate
status: completed
depends_on:
  - authorization-model
created: 2026-10-23T00:00:00Z
run_id: run-specsmd-fire-planner-3d8043-001
completed_at: 2026-10-23T00:43:14Z
---

# Work Item: Authorization interceptor and user context

## Description

Complete the Castle.DynamicProxy interceptor and the `INotaryUserContext` abstraction it reads.

## Acceptance Criteria

- [x] Interceptor in `Notary.Security.Interceptor` looks up the declared operation and checks the user's roles
- [x] Methods with no declared operation are denied; `allowAnonymous` opts out
- [x] Denial is carried through the returned task for async methods
- [x] Calls made while serving an authorized call are trusted
- [x] `INotaryUserContext` and a `NoUserContext` default for non-web hosts

## Dependencies

- authorization-model
