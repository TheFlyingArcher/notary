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

        public string CrlEndpoint { get; set; }

        public NotaryOpenIdConfiguration OpenId { get; }

        public NotaryTokenSettingsConfiguration TokenSettings { get; }
    }
}
