using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Notary.Web.Controllers;

[Route("[controller]")]
public class AuthenticateController : Controller
{
    [Route("Login")]
    [HttpGet]
    public async Task Login(string returnUrl = "/")
    {
        var authProperties = new LoginAuthenticationPropertiesBuilder()
            .WithRedirectUri(returnUrl)
            .Build();

        await HttpContext.ChallengeAsync(Auth0Constants.AuthenticationScheme, authProperties);
    }

    [Authorize]
    [Route("Logout")]
    [HttpGet]
    public async Task Logout()
    {
        var authProperties = new LogoutAuthenticationPropertiesBuilder()
            .WithRedirectUri("/")
            .Build();

        // Sign out of Auth0
        await HttpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authProperties);

        // Clear auth cookies
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}