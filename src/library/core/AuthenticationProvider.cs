namespace Notary;

/// <summary>
///     The types of authentication that is used with the system.
/// </summary>
public enum AuthenticationProvider
{
    /// <summary>
    ///     Sign on using a username/password
    /// </summary>
    System,

    /// <summary>
    ///     Sign on to the system using Active Directory
    /// </summary>
    ActiveDirectory,

    /// <summary>
    ///     Sign on to the system using OpenID. This is use for Single Sign-On
    /// </summary>
    OpenId
}