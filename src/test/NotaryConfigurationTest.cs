namespace Notary.Test;

using Notary.Configuration;

public class NotaryConfigurationTest
{
    private const string ValidApplicationKey = "0123456789abcdef";

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("http://pki.example.com/api/crl")]
    [TestCase("https://pki.example.com:8443/api/crl/")]
    public void ValidateAcceptsUnsetOrAbsoluteHttpCrlEndpoint(string? endpoint)
    {
        var config = new NotaryConfiguration { ApplicationKey = ValidApplicationKey, CrlEndpoint = endpoint };

        Assert.DoesNotThrow(config.Validate);
    }

    [TestCase(null, null)]
    [TestCase("  ", null)]
    [TestCase("https://pki.example.com", "https://pki.example.com/api/crl")]
    [TestCase("https://pki.example.com/", "https://pki.example.com/api/crl")]
    [TestCase("https://pki.example.com:8443/pki", "https://pki.example.com:8443/pki/api/crl")]
    [TestCase("https://pki.example.com/api/crl", "https://pki.example.com/api/crl")]
    [TestCase("https://pki.example.com/API/CRL/", "https://pki.example.com/API/CRL")]
    public void GetConfiguredCrlBaseUrlAppendsCrlPathOnce(string? endpoint, string? expected)
    {
        var config = new NotaryConfiguration { ApplicationKey = ValidApplicationKey, CrlEndpoint = endpoint };

        Assert.That(config.GetConfiguredCrlBaseUrl(), Is.EqualTo(expected));
    }

    [TestCase("pki.example.com/api/crl")]
    [TestCase("/api/crl")]
    [TestCase("not a url")]
    [TestCase("ftp://pki.example.com/crl")]
    [TestCase("file:///c:/crl")]
    public void ValidateRejectsCrlEndpointThatIsNotAbsoluteHttpUrl(string endpoint)
    {
        var config = new NotaryConfiguration { ApplicationKey = ValidApplicationKey, CrlEndpoint = endpoint };

        var ex = Assert.Throws<InvalidOperationException>(config.Validate);
        Assert.That(ex.Message, Does.Contain("NOTARY_CRL_ENDPOINT"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("                ")]
    [TestCase("0123456789abcde")]
    public void ValidateRejectsMissingOrShortApplicationKey(string? key)
    {
        var config = new NotaryConfiguration { ApplicationKey = key };

        var ex = Assert.Throws<InvalidOperationException>(config.Validate);
        Assert.That(ex.Message, Does.Contain("NOTARY_APP_KEY"));
        Assert.That(ex.Message, Does.Contain(NotaryConfiguration.MinimumApplicationKeyLength.ToString()));
    }

    [Test]
    public void ValidateDoesNotEchoTheApplicationKey()
    {
        var config = new NotaryConfiguration { ApplicationKey = "too-short-secret" [..10] };

        var ex = Assert.Throws<InvalidOperationException>(config.Validate);
        Assert.That(ex.Message, Does.Not.Contain("too-short-"));
    }

    [TestCase("0123456789abcdef")]
    [TestCase("a-much-longer-application-key-with-many-characters-0123456789")]
    public void ValidateAcceptsApplicationKeyAtOrAboveMinimumLength(string key)
    {
        var config = new NotaryConfiguration { ApplicationKey = key };

        Assert.DoesNotThrow(config.Validate);
    }
}
