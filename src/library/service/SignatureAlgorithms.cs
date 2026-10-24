using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace Notary.Service
{
    /// <summary>
    /// Chooses the signature algorithm for certificates and CRLs from the issuer's signing key, so the digest
    /// matches the key's strength (P-256 with SHA-256, P-384 with SHA-384, P-521 with SHA-512).
    /// </summary>
    internal static class SignatureAlgorithms
    {
        /// <summary>
        /// The signature algorithm name, as accepted by <c>Asn1SignatureFactory</c>, for the issuer's private key.
        /// </summary>
        /// <param name="issuerPrivateKey">The key that signs; the subject's key algorithm plays no part</param>
        /// <exception cref="NotaryException">The key is neither an EC nor an RSA key</exception>
        public static string ForIssuerKey(AsymmetricKeyParameter issuerPrivateKey)
        {
            switch (issuerPrivateKey)
            {
                case ECKeyParameters ecKey:
                    return ecKey.Parameters.Curve.FieldSize switch
                    {
                        384 => "SHA384WithECDSA",
                        521 => "SHA512WithECDSA",
                        _ => "SHA256WithECDSA"
                    };
                case RsaKeyParameters rsaKey:
                    return rsaKey.Modulus.BitLength switch
                    {
                        <= 2048 => "SHA256WithRSA",
                        <= 3072 => "SHA384WithRSA",
                        _ => "SHA512WithRSA"
                    };
                default:
                    throw new NotaryException($"Cannot choose a signature algorithm for a key of type {issuerPrivateKey?.GetType().Name ?? "null"}.");
            }
        }
    }
}
