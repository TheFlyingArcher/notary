namespace Notary.Security;

/// <summary>
/// An operation a user can be authorized to perform. Service methods declare the operation they perform with
/// <see cref="NotaryAuthorizeAttribute"/>; roles are granted operations in <see cref="NotaryRolePermissions"/>.
/// </summary>
public enum NotaryOperation
{
    None = 0,
    CertificateList = 1,
    CertificateGet = 2,
    CertificateDownload = 3,
    CertificateCreate = 4,
    CertificateRevoke = 5,
    CertificateAuthorityList = 6,
    CertificateAuthorityGet = 7,
    CertificateAuthorityDownload = 8,
    CertificateAuthorityCreate = 9,
    CertificateAuthorityRevoke = 10,
    AsymmetricKeyList = 11,
    AsymmetricKeyGet = 12,
    AsymmetricKeyCreate = 13,
    AsymmetricKeyDelete = 14
}
