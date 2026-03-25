namespace Notary.Configuration;

public class NotaryTokenSettingsConfiguration
{
    public string Audience { get; set; }

    public string Authority { get; set; }

    public string Issuer { get; set; }

    public string OpenIdMetadataAddress { get; set; }
}