using System;
using System.IO;
using System.Threading.Tasks;
using log4net;
using Notary.Configuration;
using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace Notary.Service;

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

    protected NotaryConfiguration Configuration { get; }

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

    public override async Task SaveAsync(AsymmetricKey entity, string updatedBySlug)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // If we're saving a key for the first time, generate a private key
        if (entity.EncryptedPrivateKey == null)
        {
            var random = GetSecureRandom();
            var keyPair = GenerateKeyPair(random, entity.KeyAlgorithm, entity.KeyCurve, entity.KeyLength);
            var encryptedPrivateKey = WriteKey(keyPair.Private, random, Configuration.ApplicationKey);
            entity.EncryptedPrivateKey = encryptedPrivateKey;
        }

        await base.SaveAsync(entity, updatedBySlug);
    }

    private AsymmetricCipherKeyPair GenerateKeyPair(SecureRandom random, Algorithm algorithm, EllipticCurve? curve,
        int? keyLength)
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
                var pr = new PemReader(tr, new PasswordFinder(encryptionKey));
                var pemObject = pr.ReadObject();

                if (algorithm == Algorithm.RSA)
                {
                    var privateKey = (RsaPrivateCrtKeyParameters)pemObject;
                    var publicKey = new RsaKeyParameters(false, privateKey.Modulus, privateKey.PublicExponent);
                    var keyPair = new AsymmetricCipherKeyPair(publicKey, privateKey);

                    return keyPair;
                }

                if (algorithm == Algorithm.EllipticCurve)
                    throw new NotImplementedException("Still figuring out how to read elliptic curve keys");
            }
        }

        throw new InvalidOperationException(
            "This should never have occured in public AsymmetricCipherKeyPair LoadKeyPair(filePath, encryptionKey, algorithm)");
    }

    /// <summary>
    ///     Write a key to PKCS #8 format
    /// </summary>
    /// <param name="keyPair">The public key</param>
    /// <param name="encryptionRandom">A secure random for password encryption</param>
    /// <param name="pkPassword">Password to encrypt the public key</param>
    private byte[] WriteKey(AsymmetricKeyParameter key, SecureRandom encryptionRandom = null, string pkPassword = null)
    {
        var generator = new Pkcs8Generator(key, Pkcs8Generator.PbeSha1_3DES);

        // Encrypt key if given
        if (encryptionRandom != null && pkPassword != null)
        {
            generator.Password = pkPassword.ToCharArray();
            generator.SecureRandom = encryptionRandom;
            generator.IterationCount = 32; // TODO: Remove magic number
        }

        byte[] keyBytes = null;

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

        return keyBytes;
    }
}