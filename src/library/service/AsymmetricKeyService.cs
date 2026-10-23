using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Text;

using log4net;

using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;
using Notary.Security;
using Notary.Configuration;

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Utilities.IO.Pem;

namespace Notary.Service
{
    public class AsymmetricKeyService : CryptographicEntityService<AsymmetricKey>, IAsymmetricKeyService
    {
        /// <summary>
        /// The PEM label of a PKCS #8 EncryptedPrivateKeyInfo
        /// </summary>
        public const string EncryptedKeyPemType = "ENCRYPTED PRIVATE KEY";

        // OWASP's 2023 recommendation for PBKDF2-HMAC-SHA256; well above the NIST SP 800-132 minimums
        private const int PbkdfIterationCount = 600_000;
        private const int SaltSize = 16;
        private const int AesBlockSize = 16;
        private const int AesKeyBits = 256;

        // Bounds applied to stored values so a tampered row cannot make a load run for ages
        private const int MinAcceptedSaltSize = 8;
        private const int MaxAcceptedIterationCount = 10_000_000;

        public AsymmetricKeyService(
            IAsymmetricKeyRepository repository,
            ILog log,
            NotaryConfiguration configuration
        ) :
            base(repository, log)
        {
            Configuration = configuration;
        }

        [NotaryAuthorize(NotaryOperation.AsymmetricKeyList)]
        public override Task<List<AsymmetricKey>> GetAllAsync() => base.GetAllAsync();

        [NotaryAuthorize(NotaryOperation.AsymmetricKeyGet)]
        public override Task<AsymmetricKey> GetAsync(string slug) => base.GetAsync(slug);

        [NotaryAuthorize(NotaryOperation.AsymmetricKeyDelete)]
        public override Task DeleteAsync(string slug, string updatedBySlug) => base.DeleteAsync(slug, updatedBySlug);

        [NotaryAuthorize(NotaryOperation.AsymmetricKeyGet)]
        public async Task<AsymmetricCipherKeyPair> GetKeyPairAsync(string slug)
        {
            var key = await GetAsync(slug);
            if (key == null)
                return null;

            var keyPair = LoadKeyPair(key.EncryptedPrivateKey, Configuration.ApplicationKey, key.KeyAlgorithm);
            return keyPair;
        }

        [NotaryAuthorize(NotaryOperation.AsymmetricKeyGet)]
        public async Task<byte[]> GetPublicKey(string slug)
        {
            var key = await GetAsync(slug);
            if (key == null)
                return null;

            var keyPair = LoadKeyPair(key.EncryptedPrivateKey, Configuration.ApplicationKey, key.KeyAlgorithm);

            // The public key is not secret; return its DER SubjectPublicKeyInfo
            return SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public).GetDerEncoded();
        }

        [NotaryAuthorize(NotaryOperation.None, allowAnonymous: true)]
        public async Task<KeyMigrationResult> MigrateStoredKeysAsync()
        {
            var password = Configuration.ApplicationKey;
            if (string.IsNullOrEmpty(password))
                throw new InvalidOperationException("The application key (NOTARY_APP_KEY) is required to migrate private keys.");

            var random = GetSecureRandom();
            int migrated = 0, alreadyEncrypted = 0, failed = 0;

            foreach (var key in await Repository.GetAllAsync())
            {
                try
                {
                    if (IsCurrentFormat(key.EncryptedPrivateKey))
                    {
                        alreadyEncrypted++;
                        continue;
                    }

                    var privateKey = ReadLegacyKey(key.EncryptedPrivateKey, password);
                    var upgraded = WriteEncryptedKey(privateKey, password, random);

                    // Never replace a readable row with one that is not: prove the new value gives back the same key
                    if (!ReadEncryptedKey(upgraded, password).Equals(privateKey))
                        throw new InvalidOperationException("The re-encrypted key did not match the original.");

                    key.EncryptedPrivateKey = upgraded;
                    key.Updated = DateTime.UtcNow;
                    key.UpdatedBySlug = "key-migration";
                    await Repository.SaveAsync(key);
                    migrated++;
                }
                catch (Exception ex)
                {
                    // The slug identifies the row; nothing about the key itself is logged
                    failed++;
                    Logger.Error($"Could not migrate stored private key '{key.Slug}': {ex.GetType().Name}: {ex.Message}");
                }
            }

            Logger.Info($"Private key migration: {migrated} migrated, {alreadyEncrypted} already encrypted, {failed} failed.");
            if (migrated > 0)
                Logger.Warn("Private keys were re-encrypted. Database backups taken before this point may still hold the previous, weaker or unencrypted form.");

            return new KeyMigrationResult(migrated, alreadyEncrypted, failed);
        }

        [NotaryAuthorize(NotaryOperation.AsymmetricKeyCreate)]
        public async override Task SaveAsync(AsymmetricKey entity, string updatedBySlug)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // If we're saving a key for the first time, generate a private key
            if (entity.EncryptedPrivateKey == null)
            {
                var random = GetSecureRandom();
                var keyPair = GenerateKeyPair(random, entity.KeyAlgorithm, entity.KeyCurve, entity.KeyLength);
                byte[] encryptedPrivateKey = WriteEncryptedKey(keyPair.Private, Configuration.ApplicationKey, random);
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
                if (!curve.HasValue)
                {
                    throw new ArgumentException("A curve is required for Elliptic Curve keys.", nameof(curve));
                }

                var domain = new ECDomainParameters(SecNamedCurves.GetByOid(GetCurveOid(curve.Value)));

                var keyGenerationParameters = new ECKeyGenerationParameters(domain, random);
                var ecGenerator = new ECKeyPairGenerator();
                ecGenerator.Init(keyGenerationParameters);

                keyPair = ecGenerator.GenerateKeyPair();
            }

            return keyPair;
        }

        private static DerObjectIdentifier GetCurveOid(EllipticCurve curve)
        {
            return curve switch
            {
                EllipticCurve.P256 => SecObjectIdentifiers.SecP256r1,
                EllipticCurve.P384 => SecObjectIdentifiers.SecP384r1,
                EllipticCurve.P521 => SecObjectIdentifiers.SecP521r1,
                _ => throw new NotSupportedException($"The elliptic curve '{curve}' is not supported.")
            };
        }

        private AsymmetricCipherKeyPair LoadKeyPair(byte[] encryptedPrivateKey, string encryptionKey, Algorithm algorithm)
        {
            var privateKey = ReadEncryptedKey(encryptedPrivateKey, encryptionKey);

            if (algorithm == Algorithm.RSA)
            {
                var rsaPrivateKey = (RsaPrivateCrtKeyParameters)privateKey;
                var publicKey = new RsaKeyParameters(false, rsaPrivateKey.Modulus, rsaPrivateKey.PublicExponent);
                return new AsymmetricCipherKeyPair(publicKey, rsaPrivateKey);
            }

            if (algorithm == Algorithm.EllipticCurve)
                return LoadEcKeyPair(privateKey);

            throw new InvalidOperationException($"Unsupported key algorithm {algorithm}.");
        }

        /// <summary>
        /// Load an Elliptic Curve key pair from a decrypted private key. The public point is derived
        /// (Q = d*G) and then validated against the curve.
        /// </summary>
        /// <param name="privateKey">The decrypted private key</param>
        /// <returns>An asymmetric key pair with both public and private EC keys</returns>
        /// <exception cref="InvalidOperationException">The key is not an EC key, or is not valid for its curve</exception>
        private static AsymmetricCipherKeyPair LoadEcKeyPair(AsymmetricKeyParameter privateKey)
        {
            if (privateKey is not ECPrivateKeyParameters ecPrivateKey)
                throw new InvalidOperationException(
                    $"The stored key is not an EC private key (found {privateKey?.GetType().Name ?? "null"}).");

            var domain = ecPrivateKey.Parameters;
            if (ecPrivateKey.D.SignValue <= 0 || ecPrivateKey.D.CompareTo(domain.N) >= 0)
                throw new InvalidOperationException("The stored EC private key is not valid for its curve: the private scalar is out of range.");

            var publicPoint = domain.G.Multiply(ecPrivateKey.D).Normalize();
            ValidateEcPublicPoint(publicPoint, domain);

            return new AsymmetricCipherKeyPair(new ECPublicKeyParameters(publicPoint, domain), ecPrivateKey);
        }

        /// <summary>
        /// Check that a public point is a usable point of the curve: finite, on the curve and of the
        /// curve's prime order (n*Q is the point at infinity).
        /// </summary>
        /// <exception cref="InvalidOperationException">The point is not valid for the curve</exception>
        internal static void ValidateEcPublicPoint(ECPoint point, ECDomainParameters domain)
        {
            if (point == null || point.IsInfinity)
                throw new InvalidOperationException("The EC public point is the point at infinity.");

            point = point.Normalize();
            if (!point.IsValid())
                throw new InvalidOperationException("The EC public point is not on the curve.");

            if (!point.Multiply(domain.N).IsInfinity)
                throw new InvalidOperationException("The EC public point does not have the order of the curve.");
        }

        /// <summary>
        /// Write a private key as a PKCS #8 EncryptedPrivateKeyInfo (RFC 5958) in PEM form, protected with
        /// PBES2 (RFC 8018): PBKDF2-HMAC-SHA256 key derivation and AES-256-CBC encryption.
        /// The key is never written unencrypted; a missing passphrase is an error.
        /// </summary>
        /// <param name="key">The private key to protect</param>
        /// <param name="password">The passphrase</param>
        /// <param name="random">A secure random used for the salt and IV</param>
        /// <returns>The "ENCRYPTED PRIVATE KEY" PEM, as ASCII bytes</returns>
        private byte[] WriteEncryptedKey(AsymmetricKeyParameter key, string password, SecureRandom random)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (!key.IsPrivate)
                throw new ArgumentException("A private key is required.", nameof(key));
            if (string.IsNullOrEmpty(password))
                throw new InvalidOperationException("The application key (NOTARY_APP_KEY) is required to protect private keys.");

            var salt = new byte[SaltSize];
            random.NextBytes(salt);
            var iv = new byte[AesBlockSize];
            random.NextBytes(iv);

            var aesKey = DeriveAesKey(password, salt, PbkdfIterationCount);
            var cipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new AesEngine()));
            cipher.Init(true, new ParametersWithIV(new KeyParameter(aesKey), iv));
            var encryptedData = cipher.DoFinal(PrivateKeyInfoFactory.CreatePrivateKeyInfo(key).GetEncoded());

            var prf = new AlgorithmIdentifier(PkcsObjectIdentifiers.IdHmacWithSha256, DerNull.Instance);
            var pbes2 = new PbeS2Parameters(
                new KeyDerivationFunc(PkcsObjectIdentifiers.IdPbkdf2, new Pbkdf2Params(salt, PbkdfIterationCount, AesKeyBits / 8, prf)),
                new EncryptionScheme(NistObjectIdentifiers.IdAes256Cbc, new DerOctetString(iv)));
            var encryptedKeyInfo = new EncryptedPrivateKeyInfo(
                new AlgorithmIdentifier(PkcsObjectIdentifiers.IdPbeS2, pbes2), encryptedData);

            using var writer = new StringWriter();
            var pemWriter = new PemWriter(writer);
            pemWriter.WriteObject(new PemObject(EncryptedKeyPemType, encryptedKeyInfo.GetEncoded()));

            var pem = writer.ToString();
            if (!pem.StartsWith($"-----BEGIN {EncryptedKeyPemType}-----", StringComparison.Ordinal))
                throw new InvalidOperationException("The private key was not encrypted; refusing to store it.");

            return Encoding.ASCII.GetBytes(pem);
        }

        /// <summary>
        /// Read a key written by <see cref="WriteEncryptedKey"/>. Only PBES2 with PBKDF2-HMAC-SHA256 and
        /// AES-256-CBC is accepted, and plaintext keys are refused.
        /// </summary>
        /// <exception cref="InvalidOperationException">The data is not an encrypted key, uses another algorithm,
        /// or could not be decrypted with the given passphrase</exception>
        private AsymmetricKeyParameter ReadEncryptedKey(byte[] pemBytes, string password)
        {
            if (pemBytes == null || pemBytes.Length == 0)
                throw new InvalidOperationException("The stored private key is empty.");
            if (string.IsNullOrEmpty(password))
                throw new InvalidOperationException("The application key (NOTARY_APP_KEY) is required to read private keys.");

            try
            {
                PemObject pemObject;
                using (var reader = new StreamReader(new MemoryStream(pemBytes), Encoding.ASCII))
                {
                    pemObject = new PemReader(reader).ReadPemObject();
                }

                if (pemObject == null || pemObject.Type != EncryptedKeyPemType)
                    throw new InvalidOperationException(
                        $"The stored private key is not an {EncryptedKeyPemType}; plaintext keys are not accepted.");

                var keyInfo = EncryptedPrivateKeyInfo.GetInstance(Asn1Object.FromByteArray(pemObject.Content));
                if (!PkcsObjectIdentifiers.IdPbeS2.Equals(keyInfo.EncryptionAlgorithm.Algorithm))
                    throw new InvalidOperationException("The stored private key does not use PBES2.");

                var pbes2 = PbeS2Parameters.GetInstance(keyInfo.EncryptionAlgorithm.Parameters);
                var kdf = pbes2.KeyDerivationFunc;
                var scheme = pbes2.EncryptionScheme;
                if (!PkcsObjectIdentifiers.IdPbkdf2.Equals(kdf.Algorithm)
                    || !NistObjectIdentifiers.IdAes256Cbc.Equals(scheme.Algorithm))
                    throw new InvalidOperationException("The stored private key does not use PBKDF2 with AES-256-CBC.");

                var pbkdf2 = Pbkdf2Params.GetInstance(kdf.Parameters);
                if (!PkcsObjectIdentifiers.IdHmacWithSha256.Equals(pbkdf2.Prf.Algorithm))
                    throw new InvalidOperationException("The stored private key does not use PBKDF2-HMAC-SHA256.");

                var salt = pbkdf2.GetSalt();
                var iterations = pbkdf2.IterationCount;
                if (salt.Length < MinAcceptedSaltSize
                    || iterations.BitLength > 31 || iterations.IntValue < 1 || iterations.IntValue > MaxAcceptedIterationCount)
                    throw new InvalidOperationException("The stored private key has unacceptable key derivation parameters.");

                var iv = Asn1OctetString.GetInstance(scheme.Parameters).GetOctets();
                if (iv.Length != AesBlockSize)
                    throw new InvalidOperationException("The stored private key has an invalid IV.");

                var aesKey = DeriveAesKey(password, salt, iterations.IntValue);
                var cipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new AesEngine()));
                cipher.Init(false, new ParametersWithIV(new KeyParameter(aesKey), iv));
                var plain = cipher.DoFinal(keyInfo.GetEncryptedData());

                return PrivateKeyFactory.CreateKey(plain);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // A wrong passphrase and corrupt data are indistinguishable; say nothing about the key itself
                Logger.Error($"Failed to decrypt a stored private key: {ex.GetType().Name}");
                throw new InvalidOperationException(
                    "The private key could not be decrypted. Check that NOTARY_APP_KEY matches the value used when the key was created.", ex);
            }
        }

        /// <summary>
        /// Whether the stored value is already an <see cref="EncryptedKeyPemType"/> in the format
        /// <see cref="WriteEncryptedKey"/> produces. Nothing is decrypted.
        /// </summary>
        private static bool IsCurrentFormat(byte[] pemBytes)
        {
            if (pemBytes == null || pemBytes.Length == 0)
                return false;

            try
            {
                PemObject pemObject;
                using (var reader = new StreamReader(new MemoryStream(pemBytes), Encoding.ASCII))
                {
                    pemObject = new PemReader(reader).ReadPemObject();
                }

                if (pemObject == null || pemObject.Type != EncryptedKeyPemType)
                    return false;

                var keyInfo = EncryptedPrivateKeyInfo.GetInstance(Asn1Object.FromByteArray(pemObject.Content));
                if (!PkcsObjectIdentifiers.IdPbeS2.Equals(keyInfo.EncryptionAlgorithm.Algorithm))
                    return false;

                var pbes2 = PbeS2Parameters.GetInstance(keyInfo.EncryptionAlgorithm.Parameters);
                return PkcsObjectIdentifiers.IdPbkdf2.Equals(pbes2.KeyDerivationFunc.Algorithm)
                    && NistObjectIdentifiers.IdAes256Cbc.Equals(pbes2.EncryptionScheme.Algorithm)
                    && PkcsObjectIdentifiers.IdHmacWithSha256.Equals(
                        Pbkdf2Params.GetInstance(pbes2.KeyDerivationFunc.Parameters).Prf.Algorithm);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Read a key stored by an earlier version: a plaintext PKCS #8 key, or one encrypted with the old
        /// PBES1 3DES scheme. Used only by the migration; normal loading goes through <see cref="ReadEncryptedKey"/>.
        /// </summary>
        private static AsymmetricKeyParameter ReadLegacyKey(byte[] pemBytes, string password)
        {
            if (pemBytes == null || pemBytes.Length == 0)
                throw new InvalidOperationException("The stored private key is empty.");

            using var reader = new StreamReader(new MemoryStream(pemBytes), Encoding.ASCII);
            var pemObject = new Org.BouncyCastle.OpenSsl.PemReader(reader, new LegacyPasswordFinder(password)).ReadObject();

            var key = pemObject switch
            {
                AsymmetricCipherKeyPair pair => pair.Private,
                AsymmetricKeyParameter parameter => parameter,
                _ => null
            };

            if (key == null || !key.IsPrivate)
                throw new InvalidOperationException("The stored value is not a private key.");

            return key;
        }

        private sealed class LegacyPasswordFinder(string password) : Org.BouncyCastle.OpenSsl.IPasswordFinder
        {
            public char[] GetPassword() => password.ToCharArray();
        }

        private static byte[] DeriveAesKey(string password, byte[] salt, int iterationCount)
        {
            var generator = new Pkcs5S2ParametersGenerator(new Sha256Digest());
            generator.Init(Encoding.UTF8.GetBytes(password), salt, iterationCount);
            return ((KeyParameter)generator.GenerateDerivedParameters("AES256", AesKeyBits)).GetKey();
        }

        protected NotaryConfiguration Configuration { get; }
    }
}
