using System.Collections.Generic;
using System.Linq;

namespace Notary.Security;

/// <summary>
/// The key sizes and curves a form can offer, narrowed to those that are at least as strong as the issuing CA
/// </summary>
public static class KeyOptions
{
    /// <summary>
    /// Every RSA key length that can be chosen
    /// </summary>
    public static IReadOnlyList<int> RsaLengths { get; } = [2048, 3072, 4096];

    /// <summary>
    /// Every curve that can be chosen
    /// </summary>
    public static IReadOnlyList<EllipticCurve> Curves { get; } = [EllipticCurve.P256, EllipticCurve.P384, EllipticCurve.P521];

    /// <summary>
    /// The RSA lengths at least as strong as the issuer; all of them when there is no issuer
    /// </summary>
    public static IReadOnlyList<int> AllowedRsaLengths(KeySpec issuer) =>
        issuer == null
            ? RsaLengths
            : [.. RsaLengths.Where(length => KeyStrength.MeetsFloor(issuer, new KeySpec(Algorithm.RSA, null, length)))];

    /// <summary>
    /// The curves at least as strong as the issuer; all of them when there is no issuer
    /// </summary>
    public static IReadOnlyList<EllipticCurve> AllowedCurves(KeySpec issuer) =>
        issuer == null
            ? Curves
            : [.. Curves.Where(curve => KeyStrength.MeetsFloor(issuer, new KeySpec(Algorithm.EllipticCurve, curve, null)))];

    /// <summary>
    /// How an offered key compares with the issuer's, for labelling the option. Null for a key that is weaker than
    /// the issuer or whose strength cannot be read, since such a key is never offered.
    /// </summary>
    public static KeyRelation? Relate(KeySpec issuer, KeySpec candidate)
    {
        var issuerBits = KeyStrength.GetSecurityBits(issuer);
        var candidateBits = KeyStrength.GetSecurityBits(candidate);
        if (!issuerBits.IsSuccess || !candidateBits.IsSuccess || candidateBits.Value < issuerBits.Value)
            return null;

        if (candidate.Algorithm == issuer.Algorithm && candidate.Curve == issuer.Curve && candidate.Length == issuer.Length)
            return KeyRelation.SameAsParent;

        return candidateBits.Value == issuerBits.Value ? KeyRelation.SameSecurityLevel : KeyRelation.Stronger;
    }

    /// <summary>
    /// The text shown for a key option, with its security level so equal levels read as equal, such as
    /// "RSA 3072 bit - 128-bit security" or "P-384 - 192-bit security"
    /// </summary>
    public static string Label(KeySpec key)
    {
        string name = key.Algorithm == Algorithm.RSA
            ? $"RSA {key.Length} bit"
            : key.Curve switch
            {
                EllipticCurve.P256 => "P-256",
                EllipticCurve.P384 => "P-384",
                EllipticCurve.P521 => "P-521",
                _ => "Elliptic Curve"
            };

        var bits = KeyStrength.GetSecurityBits(key);
        return bits.IsSuccess ? $"{name} - {bits.Value}-bit security" : name;
    }

    /// <summary>
    /// The neutral label for how an offered key relates to the issuer's
    /// </summary>
    public static string RelationText(KeyRelation relation) => relation switch
    {
        KeyRelation.SameAsParent => "Same as issuing CA",
        KeyRelation.SameSecurityLevel => "Same security level",
        _ => "Stronger"
    };

    /// <summary>
    /// The issuing CA's key and its security level, shown above the options
    /// </summary>
    public static string DescribeIssuer(KeySpec issuer) => $"Issuing CA key: {Label(issuer)}";

    /// <summary>
    /// A note telling the user how keys are compared, so equal security levels do not look like a downgrade
    /// </summary>
    public static string Describe(KeySpec issuer)
    {
        var bits = KeyStrength.GetSecurityBits(issuer);
        if (!bits.IsSuccess)
            return "The issuing CA's key strength could not be read, so no key options are offered.";

        string note = $"Keys are compared by security level, not raw size. The issuing CA's key gives {bits.Value}-bit security, so any option at that level or higher is accepted.";

        // RSA 3072 through 7679 bits share one level; the common case is a 4096-bit CA with a 3072-bit child
        return bits.Value == 128 ? $"{note} For example, RSA 3072 and RSA 4096 both give 128-bit security." : note;
    }
}

/// <summary>
/// How an offered key compares with the issuing CA's key
/// </summary>
public enum KeyRelation
{
    SameAsParent,
    SameSecurityLevel,
    Stronger
}
