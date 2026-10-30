using System.Collections.Generic;
using System.Linq;

using Notary.Contract;

namespace Notary;

/// <summary>
/// Chooses which of the certificates a CA issued are listed on its detail page.
/// </summary>
public static class IssuedCertificateFilter
{
    /// <summary>
    /// A root CA lists everything it issued, which is its child CAs. Any other CA lists leaf certificates only.
    /// </summary>
    public static List<Certificate> ForCa(IEnumerable<Certificate> issued, bool isRootCa)
    {
        return issued
            .Where(c => isRootCa || !c.IsCaCertificate)
            .OrderBy(c => c.Name, System.StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
