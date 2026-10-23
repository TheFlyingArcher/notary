using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Notary.Security;

namespace Notary.Web.Services;

/// <summary>
/// The signed-in user of the current Blazor circuit or, outside a circuit, of the current HTTP request
/// </summary>
public sealed class WebNotaryUserContext(
    AuthenticationStateProvider authenticationStateProvider,
    IHttpContextAccessor httpContextAccessor) : INotaryUserContext
{
    public ClaimsPrincipal User
    {
        get
        {
            try
            {
                // A circuit's state is already resolved, so there is nothing to wait for
                var state = authenticationStateProvider.GetAuthenticationStateAsync();
                if (state.IsCompletedSuccessfully)
                {
                    return state.Result.User;
                }
            }
            catch (InvalidOperationException)
            {
                // Outside a circuit the provider has no state
            }

            return httpContextAccessor.HttpContext?.User;
        }
    }
}
