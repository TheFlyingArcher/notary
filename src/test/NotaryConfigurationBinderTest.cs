namespace Notary.Test;

using Notary.Configuration;

public class NotaryConfigurationBinderTest
{
    private static Func<string, string?> From(Dictionary<string, string?> settings)
    {
        return key => settings.TryGetValue(key, out var value) ? value : null;
    }

    [Test]
    public void BindSetsRootDatabaseAndOpenIdSettings()
    {
        var config = new NotaryConfiguration();
        var settings = new Dictionary<string, string?>
        {
            ["NOTARY_APP_KEY"] = "0123456789abcdef",
            ["NOTARY_CRL_ENDPOINT"] = "https://pki.example.com",
            ["NOTARY_DB_HOST"] = "mongodb://db",
            ["NOTARY_DB_NAME"] = "notary",
            ["NOTARY_DB_USERNAME"] = "user",
            ["NOTARY_DB_PASSWORD"] = "pass",
            ["NOTARY_OPENID_DOMAIN"] = "tenant.us.auth0.com",
            ["NOTARY_OPENID_CLIENTID"] = "client-id",
            ["NOTARY_OPENID_CLIENTSECRET"] = "client-secret"
        };

        NotaryConfigurationBinder.Bind(config, From(settings));

        Assert.Multiple(() =>
        {
            Assert.That(config.ApplicationKey, Is.EqualTo("0123456789abcdef"));
            Assert.That(config.CrlEndpoint, Is.EqualTo("https://pki.example.com"));
            Assert.That(config.Database.Host, Is.EqualTo("mongodb://db"));
            Assert.That(config.Database.DatabaseName, Is.EqualTo("notary"));
            Assert.That(config.Database.Username, Is.EqualTo("user"));
            Assert.That(config.Database.Password, Is.EqualTo("pass"));
            Assert.That(config.OpenId.Domain, Is.EqualTo("tenant.us.auth0.com"));
            Assert.That(config.OpenId.ClientId, Is.EqualTo("client-id"));
            Assert.That(config.OpenId.ClientSecret, Is.EqualTo("client-secret"));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    public void BindKeepsTheCurrentValueWhenTheSettingIsMissingOrEmpty(string? value)
    {
        var config = new NotaryConfiguration { CrlEndpoint = "https://existing.example.com" };
        config.OpenId.Domain = "existing.us.auth0.com";
        var settings = new Dictionary<string, string?>
        {
            ["NOTARY_CRL_ENDPOINT"] = value,
            ["NOTARY_OPENID_DOMAIN"] = value
        };

        NotaryConfigurationBinder.Bind(config, From(settings));

        Assert.Multiple(() =>
        {
            Assert.That(config.CrlEndpoint, Is.EqualTo("https://existing.example.com"));
            Assert.That(config.OpenId.Domain, Is.EqualTo("existing.us.auth0.com"));
        });
    }

    [Test]
    public void BindLeavesDefaultsWhenNothingIsSet()
    {
        var config = new NotaryConfiguration();

        NotaryConfigurationBinder.Bind(config, _ => null!);

        Assert.Multiple(() =>
        {
            Assert.That(config.ApplicationKey, Is.Null);
            Assert.That(config.OpenId.Domain, Is.Empty);
            Assert.That(config.OpenId.ClientSecret, Is.Empty);
        });
    }

    [Test]
    public void BindOnlyAsksForTheSettingsTheConfigurationDeclares()
    {
        var config = new NotaryConfiguration();
        var requested = new List<string>();

        NotaryConfigurationBinder.Bind(config, key =>
        {
            requested.Add(key);
            return null!;
        });

        Assert.That(requested, Is.SupersetOf(new[]
        {
            "NOTARY_APP_KEY",
            "NOTARY_CRL_ENDPOINT",
            "NOTARY_DB_HOST",
            "NOTARY_OPENID_DOMAIN",
            "NOTARY_OPENID_CLIENTID",
            "NOTARY_OPENID_CLIENTSECRET"
        }));
        Assert.That(requested, Has.All.StartWith("NOTARY_"));
    }

    [Test]
    public void BindRejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => NotaryConfigurationBinder.Bind(null!, _ => null!));
        Assert.Throws<ArgumentNullException>(() => NotaryConfigurationBinder.Bind(new NotaryConfiguration(), null!));
    }
}
