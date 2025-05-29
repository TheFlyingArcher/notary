using Notary;
using Notary.Configuration;

namespace Notary.Test;

public abstract class NotaryTest
{
    protected NotaryConfiguration MockConfiguration()
    {
        return new NotaryConfiguration();
    }
}
