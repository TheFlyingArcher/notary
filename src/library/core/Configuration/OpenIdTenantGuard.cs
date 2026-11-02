using System;

namespace Notary.Configuration;

/// <summary>
/// Stops the application from signing in against the wrong Auth0 tenant. Each tenant is labelled
/// <see cref="DevelopmentTenant"/> or <see cref="ProductionTenant"/> and the label must suit the environment.
/// </summary>
public static class OpenIdTenantGuard
{
    /// <summary>
    /// The label of the Auth0 tenant used for local development
    /// </summary>
    public const string DevelopmentTenant = "development";

    /// <summary>
    /// The label of the Auth0 tenant used by deployed environments
    /// </summary>
    public const string ProductionTenant = "production";

    private const string DevelopmentEnvironment = "Development";

    /// <summary>
    /// Check that the configured Auth0 tenant belongs to the running environment. The development tenant is allowed
    /// only in the Development environment and the production tenant everywhere else. Does nothing when no Auth0
    /// domain is configured.
    /// </summary>
    /// <param name="openId">The OpenID settings</param>
    /// <param name="environmentName">The hosting environment name, e.g. Development or Production</param>
    /// <exception cref="InvalidOperationException">Thrown when the tenant label is missing, unknown or does not suit the environment</exception>
    public static void Validate(NotaryOpenIdConfiguration openId, string environmentName)
    {
        ArgumentNullException.ThrowIfNull(openId);

        if (string.IsNullOrWhiteSpace(openId.Domain))
        {
            return;
        }

        var tenant = openId.Tenant?.Trim().ToLowerInvariant();
        var isDevelopment = string.Equals(environmentName, DevelopmentEnvironment, StringComparison.OrdinalIgnoreCase);

        if (tenant == DevelopmentTenant && isDevelopment)
        {
            return;
        }

        if (tenant == ProductionTenant && !isDevelopment)
        {
            return;
        }

        if (tenant != DevelopmentTenant && tenant != ProductionTenant)
        {
            var found = string.IsNullOrEmpty(tenant) ? "not set" : "'" + openId.Tenant + "'";
            throw new InvalidOperationException(
                $"NOTARY_OPENID_TENANT must be '{DevelopmentTenant}' or '{ProductionTenant}' when NOTARY_OPENID_DOMAIN is set, but it is {found}.");
        }

        throw new InvalidOperationException(
            $"NOTARY_OPENID_TENANT is '{tenant}' but ASPNETCORE_ENVIRONMENT is '{environmentName}'. "
            + $"Use the '{DevelopmentTenant}' Auth0 tenant in {DevelopmentEnvironment} and the '{ProductionTenant}' tenant in every other environment.");
    }
}