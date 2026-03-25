namespace Notary.Security;

public enum NotaryOperation
{
    None = 0,
    AccountGet = 1,
    AccountSave = 2,
    AccountDelete = 3,
    AccountRegister = 4,
    CertificateGet = 5,
    CertificateSave = 6,
    CertificateDelete = 7,
    SessionAuthenticate = 8
}