using System;
using System.Reflection;

namespace Notary.Configuration;

/// <summary>
/// Fills a <see cref="NotaryConfiguration"/> from named settings, such as environment variables or user secrets
/// </summary>
public static class NotaryConfigurationBinder
{
    /// <summary>
    /// Set every property marked with <see cref="NotaryEnvironmentVariableAttribute"/> on the configuration and its
    /// database and OpenID sections. A setting that is missing or empty leaves the property as it was.
    /// </summary>
    /// <param name="config">The configuration to fill</param>
    /// <param name="lookup">Returns the value for a setting name, or null when it is not set</param>
    public static void Bind(NotaryConfiguration config, Func<string, string> lookup)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(lookup);

        BindProperties(config, lookup);
        BindProperties(config.Database, lookup);
        BindProperties(config.OpenId, lookup);
    }

    private static void BindProperties(object target, Func<string, string> lookup)
    {
        if (target == null)
        {
            return;
        }

        var properties = target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<NotaryEnvironmentVariableAttribute>();
            if (attribute == null)
            {
                continue;
            }

            var value = lookup(attribute.EnvironmentVariable);
            if (!string.IsNullOrEmpty(value))
            {
                property.SetValue(target, value);
            }
        }
    }
}
