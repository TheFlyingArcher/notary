namespace Notary.Profiles;

/// <summary>
/// The purpose a certificate authority is created for. Declared in the order shown to users.
/// </summary>
public enum CaPurpose
{
    /// <summary>
    /// Signs intermediate CAs only; never issues leaf certificates
    /// </summary>
    Root,

    /// <summary>
    /// Web server certificates (HTTPS)
    /// </summary>
    TlsSsl,

    /// <summary>
    /// User sign-in certificates (smart card, hardware token)
    /// </summary>
    UserLogin,

    /// <summary>
    /// E-mail signing and encryption
    /// </summary>
    Smime,

    /// <summary>
    /// IoT device certificates
    /// </summary>
    IoT,

    /// <summary>
    /// 802.1x / RADIUS network access certificates
    /// </summary>
    Dot1xRadius,

    /// <summary>
    /// Advanced, user-chosen usages. Discouraged.
    /// </summary>
    Custom
}
