---
id: web-user-context-401
title: Web user context and HTTP 401
intent: role-based-authorization
complexity: medium
mode: confirm
status: completed
depends_on:
  - authorization-interceptor
created: 2026-10-23T00:00:00Z
run_id: run-specsmd-fire-planner-3d8043-001
completed_at: 2026-10-23T00:43:14Z
---

# Work Item: Web user context and HTTP 401

## Description

Supply the signed-in user's roles to the interceptor and map denials to HTTP 401 in the web app.

## Acceptance Criteria

- [x] `WebNotaryUserContext` exposes the current principal's roles
- [x] `Program.cs` registers it and returns HTTP 401 for `NotaryAuthorizationException`

## Dependencies

- authorization-interceptor
