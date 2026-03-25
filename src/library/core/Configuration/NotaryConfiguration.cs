using System;
using System.Reflection;

namespace Notary.Configuration;

/// <summary>
///     Defines the configuration used for this application
/// </summary>
public class NotaryConfiguration
{
    public NotaryConfiguration()
    {
        ActiveDirectory = new NotaryActiveDirectoryConfiguration();
        Database = new NotaryDatabaseConfiguration();
        OpenId = new NotaryOpenIdConfiguration();
        TokenSettings = new NotaryTokenSettingsConfiguration();
    }

    public NotaryConfiguration(NotaryConfiguration config)
    {
        ApplicationKey = config.ApplicationKey;
        ActiveDirectory = config.ActiveDirectory;
        Authentication = config.Authentication;
        Database = config.Database;
        TokenSettings = config.TokenSettings;
    }

    public NotaryActiveDirectoryConfiguration ActiveDirectory { get; }

    [NotaryEnvironmentVariable("NOTARY_APP_KEY")]
    public string ApplicationKey { get; set; }

    [NotaryEnvironmentVariable("NOTARY_AUTH_PROVIDER")]
    public AuthenticationProvider Authentication { get; set; }

    public NotaryDatabaseConfiguration Database { get; }

    public string CrlEndpoint { get; set; }

    public NotaryOpenIdConfiguration OpenId { get; }

    public NotaryTokenSettingsConfiguration TokenSettings { get; }

    public static void SetPropertiesEnvVariable<T>(T configType) where T : class
    {
        if (configType == null)
            return;

        var type = configType.GetType();
        var typeProperties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var p in typeProperties)
        {
            var attribute = p.GetCustomAttribute<NotaryEnvironmentVariableAttribute>();
            if (attribute == null)
                continue;

            var evValue = Environment.GetEnvironmentVariable(attribute.EnvironmentVariable);
            if (!string.IsNullOrEmpty(evValue)) p.SetValue(configType, evValue);
        }
    }
}