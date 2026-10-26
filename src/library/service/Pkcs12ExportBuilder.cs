using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;

namespace Notary.Service
{
    /// <summary>
    /// Builds a password-protected PKCS #12 (RFC 7292) with current algorithms. BouncyCastle cannot choose the MAC
    /// digest or the iteration counts, so the .NET builder is used: AES-256-CBC with PBKDF2-HMAC-SHA256 (PBES2)
    /// for the key and the certificates, and an HMAC-SHA256 MAC.
    /// </summary>
    internal static class Pkcs12ExportBuilder
    {
        /// <summary>
        /// PBKDF2 iterations for the encryption and for the MAC; the OWASP 2023 value for PBKDF2-HMAC-SHA256
        /// </summary>
        public const int IterationCount = 600_000;

        private const string FriendlyNameOid = "1.2.840.113549.1.9.20";
        private const string LocalKeyIdOid = "1.2.840.113549.1.9.21";
        private const string X509CertificateTypeOid = "1.2.840.113549.1.9.22.1";

        private static readonly PbeParameters EncryptionParameters =
            new(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, IterationCount);

        /// <summary>
        /// Build the PKCS #12
        /// </summary>
        /// <param name="privateKey">The private key of the first certificate</param>
        /// <param name="chain">The certificate first, then its issuers up to the root</param>
        /// <param name="friendlyName">The name shown for the key and certificate by tools that list them</param>
        /// <param name="password">The password protecting the key, the certificates and the MAC</param>
        /// <returns>The DER encoded PKCS #12</returns>
        public static byte[] Build(AsymmetricKeyParameter privateKey, IReadOnlyList<Org.BouncyCastle.X509.X509Certificate> chain, string friendlyName, string password)
        {
            ArgumentNullException.ThrowIfNull(privateKey);
            ArgumentNullException.ThrowIfNull(chain);
            if (chain.Count == 0)
            {
                throw new ArgumentException("At least the certificate itself is required.", nameof(chain));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("A password is required to export a PKCS #12.", nameof(password));
            }

            byte[] localKeyId = System.Security.Cryptography.SHA1.HashData(chain[0].GetEncoded());

            Pkcs12SafeContents keyContents = new();
            keyContents.AddSafeBag(CreateKeyBag(privateKey, password, friendlyName, localKeyId));

            Pkcs12SafeContents certificateContents = new();
            for (int i = 0; i < chain.Count; i++)
            {
                // A certBag holds an X.509 certificate as an OCTET STRING containing its DER encoding
                AsnWriter certificate = new(AsnEncodingRules.DER);
                certificate.WriteOctetString(chain[i].GetEncoded());
                Pkcs12CertBag bag = new(new Oid(X509CertificateTypeOid), certificate.Encode());
                if (i == 0)
                {
                    AddAttributes(bag.Attributes, friendlyName, localKeyId);
                }

                certificateContents.AddSafeBag(bag);
            }

            Pkcs12Builder builder = new();
            builder.AddSafeContentsUnencrypted(keyContents);
            builder.AddSafeContentsEncrypted(certificateContents, password, EncryptionParameters);
            builder.SealWithMac(password, HashAlgorithmName.SHA256, IterationCount);

            return builder.Encode();
        }

        private static Pkcs12ShroudedKeyBag CreateKeyBag(AsymmetricKeyParameter privateKey, string password, string friendlyName, byte[] localKeyId)
        {
            byte[] pkcs8 = PrivateKeyInfoFactory.CreatePrivateKeyInfo(privateKey).GetEncoded();

            using AsymmetricAlgorithm key = privateKey switch
            {
                ECPrivateKeyParameters => ECDsa.Create(),
                RsaKeyParameters => RSA.Create(),
                _ => throw new NotSupportedException($"A {privateKey.GetType().Name} cannot be exported to PKCS #12.")
            };

            key.ImportPkcs8PrivateKey(pkcs8, out _);
            Array.Clear(pkcs8);

            Pkcs12ShroudedKeyBag bag = new(key.ExportEncryptedPkcs8PrivateKey(password, EncryptionParameters));
            AddAttributes(bag.Attributes, friendlyName, localKeyId);
            return bag;
        }

        private static void AddAttributes(CryptographicAttributeObjectCollection attributes, string friendlyName, byte[] localKeyId)
        {
            AsnWriter name = new(AsnEncodingRules.DER);
            name.WriteCharacterString(UniversalTagNumber.BMPString, friendlyName);
            attributes.Add(new Pkcs9AttributeObject(new Oid(FriendlyNameOid), name.Encode()));

            AsnWriter id = new(AsnEncodingRules.DER);
            id.WriteOctetString(localKeyId);
            attributes.Add(new Pkcs9AttributeObject(new Oid(LocalKeyIdOid), id.Encode()));
        }
    }
}
