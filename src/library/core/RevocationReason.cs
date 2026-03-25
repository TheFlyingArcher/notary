namespace Notary;

/// <summary>
///     Reasons for why a certificate was revoked
/// </summary>
public enum RevocationReason
{
    /// <summary>
    ///     Generic catch all for revocation reasons
    /// </summary>
    Unspecified = 0,

    /// <summary>
    ///     The certificate itself or its private key was compromized.
    /// </summary>
    KeyCompromized = 1,

    /// <summary>
    ///     The certificate authority was compromized. This is a Bad Thing.
    /// </summary>
    CaCompromized = 2,

    /// <summary>
    ///     A person leaves an organization
    /// </summary>
    AffiliationChanged = 3,

    /// <summary>
    ///     A person or device has changed such as their legal name or product description therefore requiring new certificate.
    /// </summary>
    Superceded = 4,

    /// <summary>
    ///     Certificate was replaced by another
    /// </summary>
    CessationOfOperation = 5,

    /// <summary>
    ///     Temporary revocation
    /// </summary>
    CertificateHold = 6,

    /// <summary>
    ///     The whole certificate authority was removed from network
    /// </summary>
    RemoveFromCrl = 8,

    /// <summary>
    ///     A person or device had privileges revoked.
    /// </summary>
    PrivilegeWithdrawn = 9,

    /// <summary>
    ///     RADIUS server handling certificate authentication compromized. Unlikely this is used.
    /// </summary>
    AaCompromized = 10
}