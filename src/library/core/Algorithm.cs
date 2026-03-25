using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Notary;

[DataContract]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Algorithm
{
    [EnumMember] RSA,

    [EnumMember] EllipticCurve
}