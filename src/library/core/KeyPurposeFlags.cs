namespace Notary;

public enum KeyPurposeFlags
{
    /// <summary>
    ///     Certificate can be used for server authentication. This is typically HTTPS
    /// </summary>
    ServerAuthentication = 0x01,

    /// <summary>
    ///     Certificate can be used to authenticate a client
    /// </summary>
    ClientAuthentication = 0x02,

    /// <summary>
    /// </summary>
    CodeSigning = 0x04,

    /// <summary>
    ///     Certificate can be used to encrypt e-mails
    /// </summary>
    EmailProtection = 0x08,

    /// <summary>
    /// </summary>
    IpsecEndSystem = 0x10,

    /// <summary>
    /// </summary>
    IpsecTunnel = 0x20,

    /// <summary>
    /// </summary>
    IpsecUser = 0x40,

    /// <summary>
    /// </summary>
    TimeStamping = 0x80,

    /// <summary>
    ///     Certificate can be used for the Online Certificate Status Protocol
    /// </summary>
    OcspSigning = 0x100,

    /// <summary>
    ///     Certificate can be used for a smart card logon (Yubikey et al)
    /// </summary>
    SmartCardLogon = 0x200,

    /// <summary>
    ///     Certificate can be associated with a MAC address
    /// </summary>
    MacAddress = 0x400
}