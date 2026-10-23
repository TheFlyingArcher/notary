using System.Security.Claims;

namespace Notary.Security;

/// <summary>
/// Supplies the user on whose behalf a service method is being called
/// </summary>
public interface INotaryUserContext
{
    /// <summary>
    /// The current user, or null when there is none
    /// </summary>
    ClaimsPrincipal User { get; }
}
