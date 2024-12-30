
namespace Notary.Configuration
{
    public class NotaryDatabaseConfiguration
    {
        [NotaryEnvironmentVariable("NOTARY_DB_HOST")]
        public string Host { get; set; }

        [NotaryEnvironmentVariable("NOTARY_DB_NAME")]
        public string DatabaseName { get; set; }

        [NotaryEnvironmentVariable("NOTARY_DB_USERNAME")]
        public string Username { get; set; }

        [NotaryEnvironmentVariable("NOTARY_DB_PASSWORD")]
        public string Password { get; set; }
    }
}
