using System;

namespace Notary.Configuration;

public class NotaryOpenIdConfiguration
{
    public NotaryOpenIdConfiguration()
    {
        ClientId = string.Empty;
        ClientSecret = string.Empty;
        Domain = string.Empty;
        Tenant = string.Empty;
    }

    [NotaryEnvironmentVariable("NOTARY_OPENID_CLIENTID")]
    public string ClientId { get; set; }

    [NotaryEnvironmentVariable("NOTARY_OPENID_CLIENTSECRET")]
    public string ClientSecret { get; set; }

    [NotaryEnvironmentVariable("NOTARY_OPENID_DOMAIN")]
    public string Domain { get; set; }

    /// <summary>
    /// Get or set which Auth0 tenant this is, <c>development</c> or <c>production</c>; see <see cref="OpenIdTenantGuard"/>
    /// </summary>
    [NotaryEnvironmentVariable("NOTARY_OPENID_TENANT")]
    public string Tenant { get; set; }

    /// <summary>
    /// Validate the settings. Nothing is required when no Auth0 domain is configured; otherwise the client ID and
    /// client secret must be set. The secret itself is never included in a message.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a required setting is missing</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Domain))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            throw new InvalidOperationException("NOTARY_OPENID_CLIENTID must be set when NOTARY_OPENID_DOMAIN is set.");
        }

        if (string.IsNullOrWhiteSpace(ClientSecret))
        {
            throw new InvalidOperationException("NOTARY_OPENID_CLIENTSECRET must be set when NOTARY_OPENID_DOMAIN is set.");
        }
    }
}
