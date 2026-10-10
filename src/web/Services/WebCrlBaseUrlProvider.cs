using Microsoft.AspNetCore.Components;
using Notary.Configuration;
using Notary.Interface.Service;

namespace Notary.Web.Services;

/// <summary>
/// Resolves the CRL base URL from the configured override, falling back to the address the application is accessed at
/// </summary>
public class WebCrlBaseUrlProvider(NotaryConfiguration configuration, NavigationManager navigationManager) : ICrlBaseUrlProvider
{
    public string GetCrlBaseUrl()
    {
        return configuration.GetConfiguredCrlBaseUrl()
            ?? navigationManager.ToAbsoluteUri(NotaryConfiguration.CrlPath).AbsoluteUri.TrimEnd('/');
    }
}
