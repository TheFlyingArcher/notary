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

## Getting roles from Auth0

Auth0 does not put a user's roles in the ID token by itself, so a user who has a role assigned but no Action behind it is signed in with no roles and is denied everything (HTTP 401, "unauthorized"). Do this once in **each** Auth0 tenant (development and production), because Actions and roles belong to a tenant:

1. **Create the roles** under User Management > Roles: `NotaryUser`, `NotaryWriter` and `NotaryAdmin`. The names are case-sensitive and must match the table above.
2. **Assign a role** to each user under User Management > Users > the user > Roles.
3. **Create an Action** under Actions > Library > Create Action > Build from scratch, with the **Login / Post Login** trigger, named for example "Add roles to token". Use this code and **Deploy** it:

   ```javascript
   exports.onExecutePostLogin = async (event, api) => {
     const assignedRoles = (event.authorization || {}).roles;
     api.idToken.setCustomClaim('http://schemas.microsoft.com/ws/2008/06/identity/claims/role', assignedRoles);
   };
   ```

   The claim name must be exactly `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`: that is the claim type ASP.NET Core treats as a role, which `[Authorize(Roles = ...)]` and `User.IsInRole` read.
4. **Attach the Action** under Actions > Triggers > post-login (or Flows > Login) by dragging it between Start and Complete, then Apply. A deployed Action does nothing until it is attached.
5. **Sign out and sign in again.** Roles are read at sign-in and kept in the session cookie, so an existing session does not see a change.

To check, sign in and open `/showprofile`. It lists every claim, and a user with the Admin role shows a `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` claim with the value `NotaryAdmin`. If the claim is missing, the Action is not deployed, not attached, or the user has no role. If it is there and a page still denies the user, check the role against the table above.

## Adding a service method

Declare its operation on the service class method. `NotaryAuthorizationTest` fails if a method that implements a
service interface has no declared operation.
