---
id: service-method-authorization
title: Declare operations on service methods
intent: role-based-authorization
complexity: medium
mode: confirm
status: completed
depends_on:
  - authorization-model
  - authorization-interceptor
created: 2026-10-23T00:00:00Z
run_id: run-specsmd-fire-planner-3d8043-001
completed_at: 2026-10-23T00:43:14Z
---

# Work Item: Declare operations on service methods

## Description

Decorate the services and register the interceptor.

## Acceptance Criteria

- [x] `[NotaryAuthorize]` on methods of the AsymmetricKey, CertificateAuthority, CertificateRevoke and Certificate services; shared `EntityService<T>` methods overridden per service
- [x] Interceptor registered on the four services in `IocRegistrations`
- [x] CRL, `InitializeAsync` and startup key migration marked `allowAnonymous`

## Dependencies

- authorization-model
- authorization-interceptor
