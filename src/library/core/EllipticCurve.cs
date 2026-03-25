using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Notary;

/// <summary>
///     The types of curves used in Elliptic Curve key generation
/// </summary>
[DataContract]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EllipticCurve
{
    /// <summary>
    ///     The P-256 curve
    /// </summary>
    [EnumMember] P256,

    /// <summary>
    ///     The P-384 curve
    /// </summary>
    [EnumMember] P384,

    /// <summary>
    ///     The P-521 curve
    /// </summary>
    [EnumMember] P521,

    /// <summary>
    ///     The P-256K curve
    /// </summary>
    [EnumMember] P256K
}