using System;

using Newtonsoft.Json;

namespace Notary.Configuration
{
    /// <summary>
    /// Defines the configuration used for this application
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
            CrlEndpoint = config.CrlEndpoint;
            CrlValidityDays = config.CrlValidityDays;
            ActiveDirectory = config.ActiveDirectory;
            Authentication = config.Authentication;
            Database = config.Database;
            TokenSettings = config.TokenSettings;
        }

        /// <summary>
        /// Validate the configuration, failing fast on values that would otherwise surface as runtime defects
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when a configured value is invalid</exception>
        public void Validate()
        {
            // The CRL endpoint is optional; when set it must be an absolute http(s) URL
            if (!string.IsNullOrWhiteSpace(CrlEndpoint)
                && (!Uri.TryCreate(CrlEndpoint, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            {
                throw new InvalidOperationException(
                    $"NOTARY_CRL_ENDPOINT '{CrlEndpoint}' is not a valid absolute http(s) URL, e.g. https://pki.example.com");
            }
        }

        /// <summary>
        /// The path, relative to the application's base address, under which CRLs are published
        /// </summary>
        public const string CrlPath = "api/crl";

        /// <summary>
        /// Get the CRL base URL derived from <see cref="CrlEndpoint"/>
        /// </summary>
        /// <returns>The URL without a trailing slash, or null if no endpoint is configured</returns>
        public string GetConfiguredCrlBaseUrl()
        {
            if (string.IsNullOrWhiteSpace(CrlEndpoint))
                return null;

            var baseUrl = CrlEndpoint.Trim().TrimEnd('/');

            // Tolerate values that already include the CRL path
            return baseUrl.EndsWith("/" + CrlPath, StringComparison.OrdinalIgnoreCase)
                ? baseUrl
                : $"{baseUrl}/{CrlPath}";
        }

        public NotaryActiveDirectoryConfiguration ActiveDirectory { get; }

        [NotaryEnvironmentVariable("NOTARY_APP_KEY")]
        public string ApplicationKey { get; set; }

        public AuthenticationProvider Authentication { get; }

        public NotaryDatabaseConfiguration Database { get; }

        /// <summary>
        /// Get or set the optional public base address of this application, e.g. https://pki.example.com.
        /// CRLs are published under <see cref="CrlPath"/> beneath it. When unset, the address the application is
        /// being accessed at is used.
        /// </summary>
        [NotaryEnvironmentVariable("NOTARY_CRL_ENDPOINT")]
        public string CrlEndpoint { get; set; }

        /// <summary>
        /// Get or set the number of days a CRL is valid for (nextUpdate - thisUpdate). Defaults to 7.
        /// </summary>
        public int CrlValidityDays { get; set; } = 7;

        public NotaryOpenIdConfiguration OpenId { get; }

        public NotaryTokenSettingsConfiguration TokenSettings { get; }
    }
}
