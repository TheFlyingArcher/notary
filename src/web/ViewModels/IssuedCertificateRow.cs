namespace Notary.Web.ViewModels;

/// <summary>
/// A certificate issued by the CA being viewed, as listed on the CA detail page
/// </summary>
public record IssuedCertificateRow(
    string Slug,
    string Name,
    string CommonName,
    DateTime Expiry,
    CertificateStatus Status,
    bool IsCaCertificate,
    bool IsRevoked);
