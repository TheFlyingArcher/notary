namespace Notary;

public enum CertificateKeyUsage
{
    EncipherOnly = 1,
    CrlSign = 2,
    KeyCertSign = 4,
    KeyAgreement = 8,
    DataEncipherment = 16,
    KeyEncipherment = 32,
    NonRepudiation = 64,
    DigitalSignature = 128,
    DecipherOnly = 32768
}