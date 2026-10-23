using System.Diagnostics;
using System.Text;

using Notary.Configuration;
using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Service;

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.X509;

namespace Notary.Test;

internal class FakeKeyRepository : FakeRepository<AsymmetricKey>, IAsymmetricKeyRepository
{
}

[TestFixture]
public class AsymmetricKeyServiceTest
{
    private const string AppKey = "unit-test-application-key-0123456789";

    private FakeKeyRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new FakeKeyRepository();
    }

    private AsymmetricKeyService CreateService(string? applicationKey = AppKey) =>
        new(_repository, Mock.Of<ILog>(), new NotaryConfiguration { ApplicationKey = applicationKey });

    private async Task<AsymmetricKey> CreateKeyAsync(AsymmetricKeyService service, Algorithm algorithm, EllipticCurve? curve = null, int? length = null)
    {
        var key = new AsymmetricKey
        {
            Active = true,
            KeyAlgorithm = algorithm,
            KeyCurve = curve,
            KeyLength = length,
            Name = "test key",
            NotBefore = DateTime.UtcNow,
            NotAfter = DateTime.UtcNow.AddDays(1)
        };

        await service.SaveAsync(key, "tester");
        return key;
    }

    private static string AsText(AsymmetricKey key) => Encoding.ASCII.GetString(key.EncryptedPrivateKey);

    private static EncryptedPrivateKeyInfo ParseEncryptedKey(AsymmetricKey key)
    {
        var base64 = string.Concat(AsText(key)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !l.StartsWith("-----")));
        return EncryptedPrivateKeyInfo.GetInstance(Asn1Object.FromByteArray(Convert.FromBase64String(base64)));
    }

    private static IEnumerable<TestCaseData> AllKeyTypes()
    {
        yield return new TestCaseData(Algorithm.RSA, null, 2048).SetName("RSA 2048");
        yield return new TestCaseData(Algorithm.EllipticCurve, EllipticCurve.P256, null).SetName("P-256");
        yield return new TestCaseData(Algorithm.EllipticCurve, EllipticCurve.P384, null).SetName("P-384");
        yield return new TestCaseData(Algorithm.EllipticCurve, EllipticCurve.P521, null).SetName("P-521");
    }

    [TestCaseSource(nameof(AllEllipticCurves))]
    public async Task Save_GeneratesAKeyForEveryEllipticCurve(EllipticCurve curve)
    {
        var key = await CreateKeyAsync(CreateService(), Algorithm.EllipticCurve, curve);

        Assert.That(AsText(key), Does.StartWith("-----BEGIN ENCRYPTED PRIVATE KEY-----"));
    }

    [Test]
    public void Save_UnknownEllipticCurve_FailsWithClearError()
    {
        var service = CreateService();

        var ex = Assert.ThrowsAsync<NotSupportedException>(
            async () => await CreateKeyAsync(service, Algorithm.EllipticCurve, (EllipticCurve)999));

        Assert.That(ex!.Message, Does.Contain("not supported"));
    }

    private static IEnumerable<EllipticCurve> AllEllipticCurves() => Enum.GetValues<EllipticCurve>();

    [TestCaseSource(nameof(AllKeyTypes))]
    public async Task Save_StoresKeyAsEncryptedPrivateKey(Algorithm algorithm, EllipticCurve? curve, int? length)
    {
        var service = CreateService();

        var key = await CreateKeyAsync(service, algorithm, curve, length);

        Assert.That(AsText(key), Does.StartWith("-----BEGIN ENCRYPTED PRIVATE KEY-----"));
        Assert.That(AsText(key), Does.Not.Contain("-----BEGIN PRIVATE KEY-----"));
    }

    [Test]
    public async Task Save_UsesPbes2WithPbkdf2HmacSha256AndAes256Cbc()
    {
        var service = CreateService();
        var key = await CreateKeyAsync(service, Algorithm.EllipticCurve, EllipticCurve.P256);

        var info = ParseEncryptedKey(key);
        var pbes2 = PbeS2Parameters.GetInstance(info.EncryptionAlgorithm.Parameters);
        var pbkdf2 = Pbkdf2Params.GetInstance(pbes2.KeyDerivationFunc.Parameters);

        Assert.Multiple(() =>
        {
            Assert.That(info.EncryptionAlgorithm.Algorithm, Is.EqualTo(PkcsObjectIdentifiers.IdPbeS2));
            Assert.That(pbes2.KeyDerivationFunc.Algorithm, Is.EqualTo(PkcsObjectIdentifiers.IdPbkdf2));
            Assert.That(pbkdf2.Prf.Algorithm, Is.EqualTo(PkcsObjectIdentifiers.IdHmacWithSha256));
            Assert.That(pbkdf2.IterationCount.IntValue, Is.EqualTo(600_000));
            Assert.That(pbkdf2.GetSalt().Length, Is.EqualTo(16));
            Assert.That(pbkdf2.KeyLength!.IntValue, Is.EqualTo(32));
            Assert.That(pbes2.EncryptionScheme.Algorithm, Is.EqualTo(NistObjectIdentifiers.IdAes256Cbc));
            Assert.That(Asn1OctetString.GetInstance(pbes2.EncryptionScheme.Parameters).GetOctets().Length, Is.EqualTo(16));
        });
    }

    [TestCaseSource(nameof(AllKeyTypes))]
    public async Task RoundTrip_LoadedKeySignsAndPublicKeyVerifies(Algorithm algorithm, EllipticCurve? curve, int? length)
    {
        var service = CreateService();
        var key = await CreateKeyAsync(service, algorithm, curve, length);

        var keyPair = await service.GetKeyPairAsync(key.Slug);

        var signatureAlgorithm = algorithm == Algorithm.RSA ? "SHA256withRSA" : "SHA256withECDSA";
        var message = Encoding.UTF8.GetBytes("notary");
        var signer = SignerUtilities.GetSigner(signatureAlgorithm);
        signer.Init(true, keyPair!.Private);
        signer.BlockUpdate(message, 0, message.Length);
        var signature = signer.GenerateSignature();

        var verifier = SignerUtilities.GetSigner(signatureAlgorithm);
        verifier.Init(false, keyPair.Public);
        verifier.BlockUpdate(message, 0, message.Length);
        Assert.That(verifier.VerifySignature(signature), Is.True);
    }

    [Test]
    public async Task Load_WithWrongApplicationKey_Throws()
    {
        var key = await CreateKeyAsync(CreateService(), Algorithm.EllipticCurve, EllipticCurve.P256);

        var wrong = CreateService("a-different-application-key");

        Assert.That(async () => await wrong.GetKeyPairAsync(key.Slug), Throws.InvalidOperationException);
    }

    [Test]
    public async Task Load_WithCorruptedCiphertext_Throws()
    {
        var service = CreateService();
        var key = await CreateKeyAsync(service, Algorithm.EllipticCurve, EllipticCurve.P256);

        // Flip a character in the middle of the base64 body
        var text = AsText(key);
        var index = text.Length / 2;
        var flipped = text[index] == 'A' ? 'B' : 'A';
        key.EncryptedPrivateKey = Encoding.ASCII.GetBytes(text.Remove(index, 1).Insert(index, flipped.ToString()));
        await _repository.SaveAsync(key);

        Assert.That(async () => await service.GetKeyPairAsync(key.Slug), Throws.InvalidOperationException);
    }

    [TestCase(null)]
    [TestCase("")]
    public void Save_WithoutApplicationKey_Throws(string? applicationKey)
    {
        var service = CreateService(applicationKey);

        Assert.That(
            async () => await CreateKeyAsync(service, Algorithm.EllipticCurve, EllipticCurve.P256),
            Throws.InvalidOperationException);
        Assert.That(_repository.GetAllAsync().Result, Is.Empty);
    }

    [Test]
    public async Task Save_SameKeyTypeTwice_ProducesDifferentSaltAndIv()
    {
        var service = CreateService();

        var first = ParseEncryptedKey(await CreateKeyAsync(service, Algorithm.EllipticCurve, EllipticCurve.P256));
        var second = ParseEncryptedKey(await CreateKeyAsync(service, Algorithm.EllipticCurve, EllipticCurve.P256));

        var firstPbes2 = PbeS2Parameters.GetInstance(first.EncryptionAlgorithm.Parameters);
        var secondPbes2 = PbeS2Parameters.GetInstance(second.EncryptionAlgorithm.Parameters);
        Assert.Multiple(() =>
        {
            Assert.That(
                Pbkdf2Params.GetInstance(firstPbes2.KeyDerivationFunc.Parameters).GetSalt(),
                Is.Not.EqualTo(Pbkdf2Params.GetInstance(secondPbes2.KeyDerivationFunc.Parameters).GetSalt()));
            Assert.That(
                Asn1OctetString.GetInstance(firstPbes2.EncryptionScheme.Parameters).GetOctets(),
                Is.Not.EqualTo(Asn1OctetString.GetInstance(secondPbes2.EncryptionScheme.Parameters).GetOctets()));
        });
    }

    [Test]
    public async Task Load_PlaintextPrivateKey_IsRefused()
    {
        var key = await CreateKeyAsync(CreateService(), Algorithm.EllipticCurve, EllipticCurve.P256);

        // Replace the stored value with an unencrypted PKCS #8 key
        var generator = new Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator();
        generator.Init(new Org.BouncyCastle.Crypto.Parameters.ECKeyGenerationParameters(NistNamedCurves.GetOid("P-256"), new SecureRandom()));
        var pkcs8 = Org.BouncyCastle.Pkcs.PrivateKeyInfoFactory.CreatePrivateKeyInfo(generator.GenerateKeyPair().Private).GetEncoded();
        var plaintext = "-----BEGIN PRIVATE KEY-----\n" + Convert.ToBase64String(pkcs8) + "\n-----END PRIVATE KEY-----\n";
        key.EncryptedPrivateKey = Encoding.ASCII.GetBytes(plaintext);
        await _repository.SaveAsync(key);

        Assert.That(async () => await CreateService().GetKeyPairAsync(key.Slug), Throws.InvalidOperationException);
    }

    [Test]
    public async Task GetPublicKey_ReturnsParsableSubjectPublicKeyInfo()
    {
        var service = CreateService();
        var key = await CreateKeyAsync(service, Algorithm.EllipticCurve, EllipticCurve.P384);
        var keyPair = await service.GetKeyPairAsync(key.Slug);

        var der = await service.GetPublicKey(key.Slug);

        var expected = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair!.Public).GetDerEncoded();
        Assert.That(der, Is.EqualTo(expected));
    }

    [Test]
    public async Task Load_TimingAtConfiguredIterations_IsRecorded()
    {
        var service = CreateService();
        var key = await CreateKeyAsync(service, Algorithm.EllipticCurve, EllipticCurve.P256);
        await service.GetKeyPairAsync(key.Slug); // warm up the JIT

        var stopwatch = Stopwatch.StartNew();
        const int loads = 3;
        for (var i = 0; i < loads; i++)
            await service.GetKeyPairAsync(key.Slug);
        stopwatch.Stop();

        var perLoadMs = stopwatch.Elapsed.TotalMilliseconds / loads;
        TestContext.Out.WriteLine($"PBES2 key load at 600,000 iterations: {perLoadMs:F0} ms per load");
        Assert.That(perLoadMs, Is.LessThan(5000));
    }

    [TestCaseSource(nameof(AllEllipticCurves))]
    public async Task LoadedPublicKey_MatchesSelfSignedCertificateSubjectPublicKeyInfo(EllipticCurve curve)
    {
        var service = CreateService();
        var key = await CreateKeyAsync(service, Algorithm.EllipticCurve, curve);
        var keyPair = await service.GetKeyPairAsync(key.Slug);

        var subject = new X509Name("CN=ec key load test");
        var generator = new X509V3CertificateGenerator();
        generator.SetSerialNumber(BigInteger.One);
        generator.SetIssuerDN(subject);
        generator.SetSubjectDN(subject);
        generator.SetNotBefore(DateTime.UtcNow.AddMinutes(-1));
        generator.SetNotAfter(DateTime.UtcNow.AddDays(1));
        generator.SetPublicKey(keyPair!.Public);
        var certificate = generator.Generate(new Asn1SignatureFactory("SHA256withECDSA", keyPair.Private));

        var certificateSpki = certificate.CertificateStructure.SubjectPublicKeyInfo.GetDerEncoded();
        Assert.Multiple(() =>
        {
            Assert.That(certificateSpki, Is.EqualTo(service.GetPublicKey(key.Slug).Result));
            Assert.That(
                certificateSpki,
                Is.EqualTo(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public).GetDerEncoded()));
            Assert.DoesNotThrow(() => certificate.Verify(certificate.GetPublicKey()));
        });
    }

    private static IEnumerable<string> NistCurveNames() => ["P-256", "P-384", "P-521"];

    private static ECDomainParameters NistDomain(string name) => new(NistNamedCurves.GetByName(name));

    [TestCaseSource(nameof(NistCurveNames))]
    public void ValidateEcPublicPoint_PointOnCurve_Passes(string curveName)
    {
        var domain = NistDomain(curveName);
        var point = domain.G.Multiply(BigInteger.ValueOf(12345)).Normalize();

        Assert.DoesNotThrow(() => AsymmetricKeyService.ValidateEcPublicPoint(point, domain));
    }

    [TestCaseSource(nameof(NistCurveNames))]
    public void ValidateEcPublicPoint_PointAtInfinity_Throws(string curveName)
    {
        var domain = NistDomain(curveName);

        var ex = Assert.Throws<InvalidOperationException>(
            () => AsymmetricKeyService.ValidateEcPublicPoint(domain.Curve.Infinity, domain));

        Assert.That(ex!.Message, Does.Contain("infinity"));
    }

    [TestCaseSource(nameof(NistCurveNames))]
    public void ValidateEcPublicPoint_PointNotOnCurve_Throws(string curveName)
    {
        var domain = NistDomain(curveName);
        var g = domain.G.Normalize();
        var offCurve = domain.Curve.CreatePoint(g.AffineXCoord.ToBigInteger(), g.AffineYCoord.ToBigInteger().Add(BigInteger.One));

        var ex = Assert.Throws<InvalidOperationException>(
            () => AsymmetricKeyService.ValidateEcPublicPoint(offCurve, domain));

        Assert.That(ex!.Message, Does.Contain("not on the curve"));
    }

    [Test]
    public void ValidateEcPublicPoint_NullPoint_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => AsymmetricKeyService.ValidateEcPublicPoint(null!, NistDomain("P-256")));
    }
}
