using System;

namespace Notary;

/// <summary>
/// How a certificate is doing, as shown to users.
/// </summary>
public enum CertificateStatus
{
    Valid,
    Expiring,
    Expired,
    Revoked
}

/// <summary>
/// Works out the <see cref="CertificateStatus"/> of a certificate.
/// </summary>
public static class CertificateStatusCalculator
{
    /// <summary>
    /// A certificate is expiring when it expires within this many days
    /// </summary>
    public const int ExpiringDays = 30;

    /// <summary>
    /// Get the status of a certificate. Expired wins over expiring, which wins over revoked.
    /// </summary>
    public static CertificateStatus Get(DateTime notAfter, DateTime? revocationDate, DateTime utcNow)
    {
        if (utcNow > notAfter)
        {
            return CertificateStatus.Expired;
        }

        if (utcNow > notAfter.AddDays(-ExpiringDays))
        {
            return CertificateStatus.Expiring;
        }

        return revocationDate.HasValue ? CertificateStatus.Revoked : CertificateStatus.Valid;
    }
}
