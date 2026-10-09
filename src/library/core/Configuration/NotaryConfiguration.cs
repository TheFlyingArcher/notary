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

        public NotaryActiveDirectoryConfiguration ActiveDirectory { get; }

        [NotaryEnvironmentVariable("NOTARY_APP_KEY")]
        public string ApplicationKey { get; set; }

        public AuthenticationProvider Authentication { get; }

        public NotaryDatabaseConfiguration Database { get; }

        /// <summary>
        /// Get or set an optional absolute base URL under which CRLs are published, e.g. http://pki.example.com/api/crl.
        /// When unset, the base URL is derived from the address the application is being accessed at.
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
