using Notary;
using Notary.Configuration;

namespace Notary.Test;

public abstract class NotaryTest
{
    protected NotaryConfiguration MockConfiguration()
    {
        return new NotaryConfiguration
        {
            ActiveDirectory = new NotaryActiveDirectoryConfiguration(),
            ApplicationKey = It.IsAny<string>(),
            Authentication = AuthenticationProvider.System,
            Database = new NotaryDatabaseConfiguration(),
            TokenSettings = new NotaryTokenSettingsConfiguration()
        };
    }
}
