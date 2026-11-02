namespace Notary.Test;

using Notary.Configuration;

public class OpenIdTenantGuardTest
{
    private const string Secret = "super-secret-value";

    private static NotaryOpenIdConfiguration OpenId(string? tenant, string domain = "tenant.us.auth0.com")
    {
        return new NotaryOpenIdConfiguration
        {
            Domain = domain,
            ClientId = "client-id",
            ClientSecret = Secret,
            Tenant = tenant!
        };
    }

    [TestCase("development", "Development")]
    [TestCase("DEVELOPMENT", "development")]
    [TestCase(" development ", "Development")]
    [TestCase("production", "Production")]
    [TestCase("production", "Staging")]
    [TestCase("Production", "production")]
    public void ValidateAcceptsATenantThatSuitsTheEnvironment(string tenant, string environment)
    {
        Assert.DoesNotThrow(() => OpenIdTenantGuard.Validate(OpenId(tenant), environment));
    }

    [Test]
    public void ValidateRejectsTheProductionTenantInDevelopment()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => OpenIdTenantGuard.Validate(OpenId("production"), "Development"));

        Assert.That(ex!.Message, Does.Contain("NOTARY_OPENID_TENANT").And.Contain("production").And.Contain("Development"));
    }

    [TestCase("Production")]
    [TestCase("Staging")]
    public void ValidateRejectsTheDevelopmentTenantOutsideDevelopment(string environment)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => OpenIdTenantGuard.Validate(OpenId("development"), environment));

        Assert.That(ex!.Message, Does.Contain("NOTARY_OPENID_TENANT").And.Contain("development").And.Contain(environment));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("staging")]
    [TestCase("prod")]
    public void ValidateRejectsAMissingOrUnknownTenantWhenADomainIsSet(string? tenant)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => OpenIdTenantGuard.Validate(OpenId(tenant), "Development"));

        Assert.That(ex!.Message, Does.Contain("NOTARY_OPENID_TENANT").And.Contain("'development' or 'production'"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void ValidateDoesNothingWhenNoDomainIsConfigured(string? domain)
    {
        Assert.DoesNotThrow(() => OpenIdTenantGuard.Validate(OpenId(null, domain!), "Production"));
    }

    [TestCase("production", "Development")]
    [TestCase("development", "Production")]
    [TestCase(null, "Production")]
    public void ValidateNeverPutsTheClientSecretInTheMessage(string? tenant, string environment)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => OpenIdTenantGuard.Validate(OpenId(tenant), environment));

        Assert.That(ex!.Message, Does.Not.Contain(Secret));
    }

    [Test]
    public void ValidateRejectsANullConfiguration()
    {
        Assert.Throws<ArgumentNullException>(() => OpenIdTenantGuard.Validate(null!, "Development"));
    }
}