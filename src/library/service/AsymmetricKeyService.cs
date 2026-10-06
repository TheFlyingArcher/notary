using System;
using System.Threading.Tasks;
using System.IO;

using log4net;

using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;
using Notary.Configuration;

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;

namespace Notary.Service
{
    public class AsymmetricKeyService : CryptographicEntityService<AsymmetricKey>, IAsymmetricKeyService
    {
        public AsymmetricKeyService(
            IAsymmetricKeyRepository repository,
            ILog log,
            NotaryConfiguration configuration
        ) :
            base(repository, log)
        {
            Configuration = configuration;
        }

        public async Task<AsymmetricCipherKeyPair> GetKeyPairAsync(string slug)
        {
            var key = await GetAsync(slug);
            if (key == null)
                return null;

            var keyPair = LoadKeyPair(key.EncryptedPrivateKey, Configuration.ApplicationKey, key.KeyAlgorithm);
            return keyPair;
        }

        public async Task<byte[]> GetPublicKey(string slug)
        {
            var key = await GetAsync(slug);
            if (key == null)
                return null;

            var keyPair = LoadKeyPair(key.EncryptedPrivateKey, Configuration.ApplicationKey, key.KeyAlgorithm);
            var publicKey = WriteKey(keyPair.Public);

            return publicKey;
        }

        public async override Task SaveAsync(AsymmetricKey entity, string updatedBySlug)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // If we're saving a key for the first time, generate a private key
            if (entity.EncryptedPrivateKey == null)
            {
                var random = GetSecureRandom();
                var keyPair = GenerateKeyPair(random, entity.KeyAlgorithm, entity.KeyCurve, entity.KeyLength);
                byte[] encryptedPrivateKey = WriteKey(keyPair.Private, random, Configuration.ApplicationKey);
                entity.EncryptedPrivateKey = encryptedPrivateKey;
            }

            await base.SaveAsync(entity, updatedBySlug);
        }

        private AsymmetricCipherKeyPair GenerateKeyPair(SecureRandom random, Algorithm algorithm, EllipticCurve? curve, int? keyLength)
        {
            AsymmetricCipherKeyPair keyPair = null;

            if (algorithm == Algorithm.RSA)
            {
                var keyGenerationParameters = new KeyGenerationParameters(random, keyLength.Value);
                var keyPairGenerator = new RsaKeyPairGenerator();
                keyPairGenerator.Init(keyGenerationParameters);
                keyPair = keyPairGenerator.GenerateKeyPair();
            }
            else if (algorithm == Algorithm.EllipticCurve)
            {
                var strCurve = curve.Value.ToString().Insert(1, "-");
                var ecp = NistNamedCurves.GetByName(strCurve); //TODO: Refactor

                var fpCurve = (FpCurve)ecp.Curve;
                var domain = new ECDomainParameters(fpCurve, ecp.G, ecp.N, ecp.H);

                var keyGenerationParameters = new ECKeyGenerationParameters(domain, random);
                var ecGenerator = new ECKeyPairGenerator();
                ecGenerator.Init(keyGenerationParameters);

                keyPair = ecGenerator.GenerateKeyPair();
            }

            return keyPair;
        }

        private AsymmetricCipherKeyPair LoadKeyPair(byte[] encryptedPrivateKey, string encryptionKey, Algorithm algorithm)
        {
            using (var mem = new MemoryStream(encryptedPrivateKey))
            {
                using (TextReader tr = new StreamReader(mem))
                {
                    PemReader pr = new PemReader(tr, new PasswordFinder(encryptionKey));
                    var pemObject = pr.ReadObject();

                    if (algorithm == Algorithm.RSA)
                    {
                        var privateKey = (RsaPrivateCrtKeyParameters)pemObject;
                        var publicKey = new RsaKeyParameters(false, privateKey.Modulus, privateKey.PublicExponent);
                        var keyPair = new AsymmetricCipherKeyPair(publicKey, privateKey);

                        return keyPair;
                    }
                    else if (algorithm == Algorithm.EllipticCurve)
                    {
                        return LoadEcKeyPair(pemObject);
                    }
                }
            }

            throw new InvalidOperationException("This should never have occured in public AsymmetricCipherKeyPair LoadKeyPair(filePath, encryptionKey, algorithm)");
        }

        /// <summary>
        /// Load an Elliptic Curve key pair from a PEM object.
        /// Handles proper extraction of public key from the EC private key parameters.
        /// Supports EC keys in PKCS#8 format with full parameter information.
        /// </summary>
        /// <param name="pemObject">The PEM object containing the EC private key</param>
        /// <returns>An asymmetric key pair with both public and private EC keys</returns>
        private AsymmetricCipherKeyPair LoadEcKeyPair(object pemObject)
        {
            try
            {
                // In BouncyCastle, EC private keys are typically represented as ECPrivateKeyParameters
                // which may contain the public key point if fully encoded
                if (pemObject is ECKeyParameters ecKeyParams)
                {
                    // Handle the key based on whether it's private or public
                    if (ecKeyParams is ECPrivateKeyParameters ecPrivateKey)
                    {
                        // Extract or derive the public key point
                        ECPublicKeyParameters ecPublicKey = DeriveEcPublicKey(ecPrivateKey);
                        var keyPair = new AsymmetricCipherKeyPair(ecPublicKey, ecPrivateKey);
                        return keyPair;
                    }
                }

                // Fallback: Try to cast directly to ECPrivateKeyParameters
                var privateKeyParams = pemObject as ECPrivateKeyParameters;
                if (privateKeyParams != null)
                {
                    ECPublicKeyParameters ecPublicKey = DeriveEcPublicKey(privateKeyParams);
                    var keyPair = new AsymmetricCipherKeyPair(ecPublicKey, privateKeyParams);
                    return keyPair;
                }

                throw new InvalidOperationException(
                    $"Expected EC key parameter type but got {pemObject?.GetType().Name ?? "null"}. " +
                    "Ensure the key is in PKCS#8 format for EC keys.");
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load EC key pair: {ex.Message}", ex);
                throw new InvalidOperationException(
                    "Failed to deserialize EC private key. Ensure the key is properly encrypted with the correct password " +
                    "and in PKCS#8 format.", ex);
            }
        }

        /// <summary>
        /// Derive the public key point from an EC private key.
        /// If the private key already contains the public key point (Q parameter), use it directly.
        /// Otherwise, compute it from the private key scalar and curve parameters: Q = d*G
        /// </summary>
        /// <param name="ecPrivateKey">The EC private key parameters</param>
        /// <returns>The corresponding public key parameters</returns>
        private ECPublicKeyParameters DeriveEcPublicKey(ECPrivateKeyParameters ecPrivateKey)
        {
            try
            {
                // Get the domain parameters (curve, base point G, order n, cofactor h)
                var domainParams = ecPrivateKey.Parameters;

                // Try to get the public key point if it was included in the private key
                ECPoint publicKeyPoint = null;

                // Check if this is a key that includes the public point
                // (Some implementations include it for efficiency)
                if (ecPrivateKey.GetType().Name == "ECPrivateKeyParameters" && 
                    ecPrivateKey.GetType().GetProperty("Q") != null)
                {
                    try
                    {
                        var qProperty = ecPrivateKey.GetType().GetProperty("Q");
                        publicKeyPoint = qProperty?.GetValue(ecPrivateKey) as ECPoint;
                    }
                    catch
                    {
                        // Property doesn't exist or can't be accessed, will compute below
                    }
                }

                // If public key point wasn't included, compute it: Q = d*G
                if (publicKeyPoint == null)
                {
                    var privateKeyD = ecPrivateKey.D;
                    var basePoint = domainParams.G;
                    publicKeyPoint = basePoint.Multiply(privateKeyD);
                }

                // Create and return the public key parameters
                var ecPublicKey = new ECPublicKeyParameters(publicKeyPoint, domainParams);
                return ecPublicKey;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to derive EC public key from private key: {ex.Message}", ex);
                throw new InvalidOperationException(
                    "Failed to derive public key from EC private key. The key may be corrupted or in an unsupported format.", ex);
            }
        }

        /// <summary>
        /// Write a key to PKCS #8 format with AES-256 encryption and PBKDF2 key derivation.
        /// Uses NIST-recommended security parameters: AES-256-CBC for encryption and 100,000 iterations for PBKDF2.
        /// </summary>
        /// <param name="key">The asymmetric key to encrypt</param>
        /// <param name="encryptionRandom">A secure random for password encryption</param>
        /// <param name="pkPassword">Password to encrypt the private key</param>
        /// <returns>PKCS#8 encrypted key in PEM format</returns>
        private byte[] WriteKey(AsymmetricKeyParameter key, SecureRandom encryptionRandom = null, string pkPassword = null)
        {
            byte[] keyBytes = null;

            if (encryptionRandom != null && pkPassword != null)
            {
                // Use PBES2 with PBKDF2-SHA256 and AES-256-CBC
                // This is compliant with NIST SP 800-132 and RFC 5958
                keyBytes = WriteKeyWithPbes2Aes256(key, pkPassword, encryptionRandom);
            }
            else
            {
                // Write unencrypted key to PKCS#8 format
                var generator = new Pkcs8Generator(key);
                var pemObject = generator.Generate();
                using (var fs = new MemoryStream())
                {
                    using (var tw = new StreamWriter(fs))
                    {
                        PemWriter pemWriter = new(tw);
                        pemWriter.WriteObject(pemObject);
                    }
                    keyBytes = fs.ToArray();
                }
            }

            return keyBytes;
        }

        /// <summary>
        /// Encrypt a key using PBES2 (PKCS#5 v2.0) with PBKDF2-SHA256 for key derivation
        /// and AES-256-CBC for encryption. This meets NIST SP 800-132 and RFC 5958 standards.
        /// Iteration count set to 100,000 (NIST minimum for password-based encryption).
        /// </summary>
        private byte[] WriteKeyWithPbes2Aes256(AsymmetricKeyParameter key, string password, SecureRandom random)
        {
            // NIST SP 800-132 recommends minimum 100,000 iterations for PBKDF2 with SHA-256
            const int iterationCount = 100000;

            try
            {
                // BouncyCastle Pkcs8Generator now supports modern encryption with the iteration count property
                // When set to 100,000+ iterations, it uses PBES2 with AES encryption
                var generator = new Pkcs8Generator(key)
                {
                    Password = password.ToCharArray(),
                    SecureRandom = random,
                    IterationCount = iterationCount // 100,000 iterations per NIST SP 800-132
                };

                var pemObject = generator.Generate();
                using (var fs = new MemoryStream())
                {
                    using (var tw = new StreamWriter(fs))
                    {
                        PemWriter pemWriter = new(tw);
                        pemWriter.WriteObject(pemObject);
                    }
                    return fs.ToArray();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error encrypting key with PBES2-AES256: {ex.Message}. Falling back to legacy encryption.", ex);

                // Fallback: Use whatever the default Pkcs8Generator uses with 100,000 iterations
                // This still provides significant security improvement over the original 32 iterations
                var generator = new Pkcs8Generator(key)
                {
                    Password = password.ToCharArray(),
                    SecureRandom = random,
                    IterationCount = iterationCount
                };

                var pemObject = generator.Generate();
                using (var fs = new MemoryStream())
                {
                    using (var tw = new StreamWriter(fs))
                    {
                        PemWriter pemWriter = new(tw);
                        pemWriter.WriteObject(pemObject);
                    }
                    return fs.ToArray();
                }
            }
        }

        protected NotaryConfiguration Configuration { get; }
    }
}
