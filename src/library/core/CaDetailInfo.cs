using Notary.Contract;
using Notary.Profiles;

namespace Notary;

/// <summary>
/// Facts about a certificate authority shown on its detail page.
/// </summary>
public static class CaDetailInfo
{
    /// <summary>
    /// The kind of CA shown to users, such as Root or TLS/SSL. A CA created before purposes existed is Legacy.
    /// </summary>
    public static string KindName(CertificateAuthority ca)
    {
        return ca.Purpose.HasValue ? CaProfileCatalog.Get(ca.Purpose).DisplayName : "Legacy";
    }

    /// <summary>
    /// Whether the CA is a root: it has the Root purpose, or no parent above it. A legacy CA has no purpose,
    /// so having no parent is what marks it as a root, the same way <see cref="CaHierarchy"/> orders it.
    /// </summary>
    public static bool IsRoot(CertificateAuthority ca)
    {
        return ca.Purpose == CaPurpose.Root
            || string.IsNullOrEmpty(ca.ParentCaSlug)
            || ca.ParentCaSlug == ca.Slug;
    }
}
