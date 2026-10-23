# Role-based authorization

Notary authorizes in two layers. Pages carry `[Authorize(Roles = ...)]`; the service methods behind them check
the caller again, so a role that gets past a page is still denied an operation it was not granted.

## How it works

- Each service method declares the operation it performs with `[NotaryAuthorize(NotaryOperation.CertificateList)]`
  (`Notary.Security`). The attribute sits on the method of the service **class**, for example `CertificateService`.
- The shared `EntityService<T>` methods (`GetAllAsync`, `GetAsync`, `SaveAsync`, `DeleteAsync`) are overridden in each
  service so that each can declare the operation for its own entity.
- `Notary.Security.Interceptor.NotaryAuthorization` is a Castle DynamicProxy interceptor, registered on every service in
  `IocRegistrations`. It looks up the operation, reads the current user from `INotaryUserContext`, and checks
  `NotaryRolePermissions`. A denied call throws `NotaryAuthorizationException` (`StatusCode` 401); for `Task`-returning
  methods the exception is carried by the returned task. The web app turns it into an HTTP 401.
- A method with no declared operation is denied to everyone. `[NotaryAuthorize(NotaryOperation.None, allowAnonymous: true)]`
  opts a method out of the check (the published CRL, `InitializeAsync`, the startup key migration).
- Calls one service makes to another while serving an authorized call are trusted, so a Writer issuing a certificate
  does not also need key operations.

## Roles and operations

| Role | Operations |
| --- | --- |
| `NotaryUser` | List, get and download certificates and certificate authorities; get a key |
| `NotaryWriter` | Everything `NotaryUser` can, plus create and revoke certificates |
| `NotaryAdmin` | Every operation, including creating certificate authorities and managing keys |

A user holding several roles has the union. The mapping lives in `NotaryRolePermissions`.

## Adding a service method

Declare its operation on the service class method. `NotaryAuthorizationTest` fails if a method that implements a
service interface has no declared operation.
