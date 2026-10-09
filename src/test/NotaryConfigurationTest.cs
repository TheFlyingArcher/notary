namespace Notary.Test;

using Notary.Configuration;

public class NotaryConfigurationTest
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("http://pki.example.com/api/crl")]
    [TestCase("https://pki.example.com:8443/api/crl/")]
    public void ValidateAcceptsUnsetOrAbsoluteHttpCrlEndpoint(string? endpoint)
    {
        var config = new NotaryConfiguration { CrlEndpoint = endpoint };

        Assert.DoesNotThrow(config.Validate);
    }

    [TestCase("pki.example.com/api/crl")]
    [TestCase("/api/crl")]
    [TestCase("not a url")]
    [TestCase("ftp://pki.example.com/crl")]
    [TestCase("file:///c:/crl")]
    public void ValidateRejectsCrlEndpointThatIsNotAbsoluteHttpUrl(string endpoint)
    {
        var config = new NotaryConfiguration { CrlEndpoint = endpoint };

        var ex = Assert.Throws<InvalidOperationException>(config.Validate);
        Assert.That(ex.Message, Does.Contain("NOTARY_CRL_ENDPOINT"));
    }
}
