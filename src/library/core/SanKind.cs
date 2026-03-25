using System.Runtime.Serialization;

namespace Notary;

/// <summary>
///     The kinds of SAN entries that can be used in a X.509 certificate
/// </summary>
[DataContract]
public enum SanKind
{
    /// <summary>
    ///     A DNS SAN entry
    /// </summary>
    [EnumMember] Dns,

    /// <summary>
    ///     An email SAN entry
    /// </summary>
    [EnumMember] Email,

    /// <summary>
    ///     An IP address SAN entry
    /// </summary>
    [EnumMember] IpAddress,

    /// <summary>
    ///     A user principal SAN entry
    /// </summary>
    [EnumMember] UserPrincipal,

    /// <summary>
    ///     A uniform resource indicator SAN entry
    /// </summary>
    [EnumMember] Uri
}