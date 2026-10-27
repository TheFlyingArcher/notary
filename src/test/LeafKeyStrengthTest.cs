using Notary.Configuration;
using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;
using Notary.Service;
using Org.BouncyCastle.Asn1.Sec;

namespace Notary.Test;

/// <summary>
/// A certificate may never be weaker than the CA that issues it, whatever the key types are
/// </summary>
public class LeafKeyStrengthTest
{
    private FakeKeyService _keys;
    private FakeCertificateRepository _certificateRepo;
    private CertificateService _certificates;

    [SetUp]
    public void SetUp()
    {
        var config = new NotaryConfiguration();
        var baseUrlProvider = new Mock<ICrlBaseUrlProvider>();
        baseUrlProvider.Setup(p => p.GetCrlBaseUrl()).Returns("http://pki.test/api/crl");

        _keys = new FakeKeyService();
        _certificateRepo = new FakeCertificateRepository();
        _certificates = new CertificateService(config, _keys, _certificateRepo, new FakeCertificateAuthorityRepository(), baseUrlProvider.Object, new Mock<ILog>().Object);
    }

    [TestCase(EllipticCurve.P384, EllipticCurve.P256)]
    [TestCase(EllipticCurve.P521, EllipticCurve.P384)]
    public async Task EcLeafWeakerThanEcCa_IsRejectedBeforeAnyKeyIsMade(EllipticCurve issuerCurve, EllipticCurve leafCurve)
    {
        var root = await IssueAsync("root", Algorithm.EllipticCurve, issuerCurve, null, isCa: true);
        var keysBefore = _keys.Count;

        var result = await TryIssueAsync("leaf", Algorithm.EllipticCurve, leafCurve, null, parent: root);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("at least as strong as its issuer"));
        Assert.That(_keys.Count, Is.EqualTo(keysBefore), "No key is generated for a rejected certificate");
        Assert.That(await _certificateRepo.GetCertificatesByCaAsync(root.Slug), Is.Empty, "Nothing is saved");
    }

    [Test]
    public async Task RsaLeafWeakerThanEcCa_IsRejected()
    {
        var root = await IssueAsync("root", Algorithm.EllipticCurve, EllipticCurve.P256, null, isCa: true);

        var result = await TryIssueAsync("leaf", Algorithm.RSA, null, 2048, parent: root);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("112-bit").And.Contain("128-bit"));
    }

    [Test]
    public async Task EcLeafStrongerThanRsaCa_IsAccepted()
    {
        var root = await IssueAsync("root", Algorithm.RSA, null, 2048, isCa: true);

        var result = await TryIssueAsync("leaf", Algorithm.EllipticCurve, EllipticCurve.P256, null, parent: root);

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [Test]
    public async Task RsaLeafAtEqualStrengthToEcCa_IsAccepted()
    {
        var root = await IssueAsync("root", Algorithm.EllipticCurve, EllipticCurve.P256, null, isCa: true);

        var result = await TryIssueAsync("leaf", Algorithm.RSA, null, 3072, parent: root);

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [Test]
    public async Task LeafAtTheSameStrengthAndType_IsAccepted()
    {
        var root = await IssueAsync("root", Algorithm.EllipticCurve, EllipticCurve.P384, null, isCa: true);

        var result = await TryIssueAsync("leaf", Algorithm.EllipticCurve, EllipticCurve.P384, null, parent: root);

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [Test]
    public async Task ChildCaCertificateWeakerThanItsIssuer_IsRejected()
    {
        var root = await IssueAsync("root", Algorithm.EllipticCurve, EllipticCurve.P384, null, isCa: true);

        var result = await TryIssueAsync("intermediate", Algorithm.EllipticCurve, EllipticCurve.P256, null, isCa: true, parent: root);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task SelfSignedCertificate_HasNoIssuerToCompareWith()
    {
        var result = await TryIssueAsync("self", Algorithm.RSA, null, 2048);

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [Test]
    public async Task EcLeafWithoutACurve_IsRejectedAsInvalid()
    {
        var root = await IssueAsync("root", Algorithm.EllipticCurve, EllipticCurve.P256, null, isCa: true);

        var result = await TryIssueAsync("leaf", Algorithm.EllipticCurve, null, null, parent: root);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [TestCase(EllipticCurve.P256, 128)]
    [TestCase(EllipticCurve.P384, 192)]
    [TestCase(EllipticCurve.P521, 256)]
    public void KeySpecs_EcKey_MapsTheCurve(EllipticCurve expected, int bits)
    {
        var generator = new ECKeyPairGenerator();
        generator.Init(new ECKeyGenerationParameters(
            expected switch
            {
                EllipticCurve.P256 => SecObjectIdentifiers.SecP256r1,
                EllipticCurve.P384 => SecObjectIdentifiers.SecP384r1,
                _ => SecObjectIdentifiers.SecP521r1
            },
            new SecureRandom()));

        var spec = KeySpecs.FromKey(generator.GenerateKeyPair().Public);

        Assert.That(spec.IsSuccess, Is.True, spec.Error);
        Assert.That(spec.Value.Algorithm, Is.EqualTo(Algorithm.EllipticCurve));
        Assert.That(spec.Value.Curve, Is.EqualTo(expected));
        Assert.That(Notary.Security.KeyStrength.GetSecurityBits(spec.Value).Value, Is.EqualTo(bits));
    }

    [Test]
    public void KeySpecs_RsaKey_ReadsTheModulusSize()
    {
        var generator = new RsaKeyPairGenerator();
        generator.Init(new KeyGenerationParameters(new SecureRandom(), 2048));

        var spec = KeySpecs.FromKey(generator.GenerateKeyPair().Public);

        Assert.That(spec.IsSuccess, Is.True, spec.Error);
        Assert.That(spec.Value.Algorithm, Is.EqualTo(Algorithm.RSA));
        Assert.That(spec.Value.Length, Is.EqualTo(2048));
    }

    [Test]
    public void KeySpecs_UnsupportedKey_IsInvalid()
    {
        var spec = KeySpecs.FromKey(new Ed25519PrivateKeyParameters(new SecureRandom()));

        Assert.That(spec.IsSuccess, Is.False);
        Assert.That(spec.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    private async Task<Certificate> IssueAsync(string name, Algorithm algorithm, EllipticCurve? curve, int? keySize, bool isCa = false, Certificate parent = null)
    {
        var result = await TryIssueAsync(name, algorithm, curve, keySize, isCa, parent);
        Assert.That(result.IsSuccess, Is.True, result.Error);
        return result.Value;
    }

    private async Task<Result<Certificate>> TryIssueAsync(string name, Algorithm algorithm, EllipticCurve? curve, int? keySize, bool isCa = false, Certificate parent = null)
    {
        var usage = isCa
            ? (int)(CertificateKeyUsage.KeyCertSign | CertificateKeyUsage.CrlSign)
            : (int)CertificateKeyUsage.DigitalSignature;

        return await _certificates.IssueCertificateAsync(new CertificateRequest
        {
            CertificateKeyUsageFlags = [usage],
            Curve = curve,
            ExtendedKeyUsages = [],
            IsCaCertificate = isCa,
            KeyAlgorithm = algorithm,
            KeySize = keySize,
            Name = name,
            NotAfter = parent?.NotAfter ?? DateTime.UtcNow.AddYears(1),
            NotBefore = DateTime.UtcNow.AddMinutes(-5),
            ParentCertificateSlug = parent?.Slug,
            RequestedBySlug = "tester",
            Subject = new DistinguishedName { CommonName = name },
            SubjectAlternativeNames = []
        });
    }
}
