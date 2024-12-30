using System;

namespace Notary.Configuration;

public class NotaryOpenIdConfiguration
{
    public NotaryOpenIdConfiguration()
    {
        ClientId = string.Empty;
        ClientSecret = string.Empty;
        Domain = string.Empty;
    }

    [NotaryEnvironmentVariable("NOTARY_OPENID_CLIENTID")]
    public string ClientId { get; set; }

    [NotaryEnvironmentVariable("NOTARY_OPENID_CLIENTSECRET")]
    public string ClientSecret { get; set; }

    [NotaryEnvironmentVariable("NOTARY_OPENID_DOMAIN")]
    public string Domain { get; set; }
}
