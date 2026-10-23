using System.Security.Claims;
using Notary.Security;

namespace Notary.Service;

/// <summary>
/// The user context of a host that has no signed-in user, such as the setup tool
/// </summary>
internal sealed class NoUserContext : INotaryUserContext
{
    public ClaimsPrincipal User => null;
}
