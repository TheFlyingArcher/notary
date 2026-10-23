---
id: authorization-docs-tests
title: Authorization tests and docs
intent: role-based-authorization
complexity: medium
mode: confirm
status: completed
depends_on:
  - service-method-authorization
  - web-user-context-401
created: 2026-10-23T00:00:00Z
run_id: run-specsmd-fire-planner-3d8043-001
completed_at: 2026-10-23T00:43:14Z
---

# Work Item: Authorization tests and docs

## Description

Test the role matrix, interceptor and wiring, and document the model.

## Acceptance Criteria

- [x] `NotaryAuthorizationTest` covers the role matrix and interceptor, and fails if a service-interface method has no declared operation
- [x] `NotaryAuthorizationWiringTest` verifies Autofac wiring
- [x] `docs/authorization.md` explains the layers, roles and how to add a method
- [x] Suite passes (383 tests) and UAT done in Visual Studio with each role

## Dependencies

- service-method-authorization
- web-user-context-401
