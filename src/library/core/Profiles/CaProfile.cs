using System.Collections.Generic;

namespace Notary.Profiles;

/// <summary>
/// What a CA of a given purpose carries and what the certificates it issues may carry
/// </summary>
/// <param name="Purpose">The CA purpose</param>
/// <param name="DisplayName">The name shown to users</param>
/// <param name="Description">A short description shown to users</param>
/// <param name="IsDiscouraged">True if the purpose should be marked as discouraged in the UI</param>
/// <param name="CaKeyUsages">Key usages on the CA certificate</param>
/// <param name="CaExtendedKeyUsages">Extended key usage OIDs on the CA certificate; empty means no restriction</param>
/// <param name="IssuesCaCertificates">True if the CA may issue CA certificates</param>
/// <param name="IssuesLeafCertificates">True if the CA may issue leaf certificates</param>
/// <param name="RestrictsLeafUsages">True if leaf certificates are limited to the leaf usages below</param>
/// <param name="LeafKeyUsages">Key usages permitted on leaf certificates</param>
/// <param name="LeafExtendedKeyUsages">Extended key usage OIDs permitted on leaf certificates</param>
public sealed record CaProfile(
    CaPurpose Purpose,
    string DisplayName,
    string Description,
    bool IsDiscouraged,
    IReadOnlyList<CertificateKeyUsage> CaKeyUsages,
    IReadOnlyList<string> CaExtendedKeyUsages,
    bool IssuesCaCertificates,
    bool IssuesLeafCertificates,
    bool RestrictsLeafUsages,
    IReadOnlyList<CertificateKeyUsage> LeafKeyUsages,
    IReadOnlyList<string> LeafExtendedKeyUsages)
{
    /// <summary>
    /// True when the CA certificate carries extended key usages that child CAs must stay within
    /// </summary>
    public bool RestrictsExtendedKeyUsages => CaExtendedKeyUsages.Count > 0;

    /// <summary>
    /// True when the CA issues leaf certificates and fixes the usages they carry, so they are applied for the user
    /// instead of being chosen
    /// </summary>
    public bool LocksLeafUsages => IssuesLeafCertificates && RestrictsLeafUsages;

    /// <summary>
    /// The key usages permitted on leaf certificates whose subject key uses the given algorithm. An EC key cannot
    /// encipher (RFC 5480 section 3), so KeyEncipherment is replaced by KeyAgreement where the profile encrypts and
    /// dropped otherwise, and DataEncipherment is dropped.
    /// </summary>
    /// <param name="algorithm">The subject key algorithm</param>
    public IReadOnlyList<CertificateKeyUsage> GetLeafKeyUsages(Algorithm algorithm)
    {
        if (algorithm != Algorithm.EllipticCurve)
        {
            return LeafKeyUsages;
        }

        List<CertificateKeyUsage> usages = [];
        foreach (CertificateKeyUsage usage in LeafKeyUsages)
        {
            if (usage == CertificateKeyUsage.KeyEncipherment)
            {
                // Only the S/MIME profile asks for more than signing; its encryption certificates use key agreement
                if (Purpose == CaPurpose.Smime)
                {
                    usages.Add(CertificateKeyUsage.KeyAgreement);
                }
            }
            else if (usage != CertificateKeyUsage.DataEncipherment)
            {
                usages.Add(usage);
            }
        }

        return usages;
    }
}
