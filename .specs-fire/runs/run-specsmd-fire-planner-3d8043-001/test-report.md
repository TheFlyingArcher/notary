---
run: run-specsmd-fire-planner-3d8043-001
intent: role-based-authorization
generated: 2026-10-23T00:43:14Z
status: passed
---

# Test Report: Role-based authorization (backfilled)

| Category | Passed | Failed | Skipped |
|----------|--------|--------|---------|
| Unit | 383 | 0 | 0 |

Per PR #55: `dotnet test` passing, including the role matrix, interceptor and Autofac wiring tests (`NotaryAuthorizationTest`, `NotaryAuthorizationWiringTest`). UAT was done in Visual Studio with each role. Coverage not measured.

```bash
dotnet test ./src/test/
```
