using Notary.Security;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace Notary.Service
{
    /// <summary>
    /// Describes a real key in the terms the key strength rule uses, so an issuer is judged by the key that signs
    /// rather than by whatever was recorded about it
    /// </summary>
    internal static class KeySpecs
    {
        /// <summary>
        /// The algorithm, curve and size of an EC or RSA key; a key of another kind, or an EC curve Notary does not
        /// issue, is reported as invalid.
        /// </summary>
        public static Result<KeySpec> FromKey(AsymmetricKeyParameter key)
        {
            switch (key)
            {
                case ECKeyParameters ecKey:
                    EllipticCurve? curve = ecKey.Parameters.Curve.FieldSize switch
                    {
                        256 => EllipticCurve.P256,
                        384 => EllipticCurve.P384,
                        521 => EllipticCurve.P521,
                        _ => null
                    };

                    return curve.HasValue
                        ? Result<KeySpec>.Ok(new KeySpec(Algorithm.EllipticCurve, curve, null))
                        : Result<KeySpec>.Fail(ResultStatus.Invalid, $"The issuing key uses an unsupported {ecKey.Parameters.Curve.FieldSize}-bit curve.");

                case RsaKeyParameters rsaKey:
                    return Result<KeySpec>.Ok(new KeySpec(Algorithm.RSA, null, rsaKey.Modulus.BitLength));

                default:
                    return Result<KeySpec>.Fail(ResultStatus.Invalid, "The issuing key is neither an EC nor an RSA key.");
            }
        }
    }
}
