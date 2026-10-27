using Notary.Configuration;
using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;
using Notary.Service;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.X509;

namespace Notary.Test;

/// <summary>
/// Builds real chains that mix RSA and Elliptic Curve keys, and checks that nothing is issued weaker than its issuer
/// </summary>
public class MixedChainTest
{
    private FakeKeyService _keys;
    private FakeCertificateRepository _certificateRepo;
    private FakeCertificateAuthorityRepository _caRepo;
    private CertificateService _certificates;
    private CertificateAuthorityService _authorities;

    [SetUp]
    public void SetUp()
    {
        var config = new NotaryConfiguration();
        var baseUrlProvider = new Mock<ICrlBaseUrlProvider>();
        baseUrlProvider.Setup(p => p.GetCrlBaseUrl()).Returns("http://pki.test/api/crl");
        var log = new Mock<ILog>().Object;

        _keys = new FakeKeyService();
        _certificateRepo = new FakeCertificateRepository();
        _caRepo = new FakeCertificateAuthorityRepository();
        _certificates = new CertificateService(config, _keys, _certificateRepo, _caRepo, baseUrlProvider.Object, log);
        _authorities = new CertificateAuthorityService(_caRepo, _certificates, config, log);
    }

    [Test]
    public async Task RsaRoot_EcIntermediate_BothKeyTypesBelow_VerifyAndSignWithTheIssuersAlgorithm()
    {
        // RSA-3072 and P-256 are both 128-bit, so the chain changes key type without getting weaker
        var root = await CreateCaAsync(Algorithm.RSA, null, 3072);
        var intermediate = await CreateCaAsync(Algorithm.EllipticCurve, EllipticCurve.P256, null, root);
        var rsaLeaf = await IssueLeafAsync(intermediate, Algorithm.RSA, null, 3072);
        var ecLeaf = await IssueLeafAsync(intermediate, Algorithm.EllipticCurve, EllipticCurve.P256, null);

        var rootX509 = await ParseCaAsync(root);
        var intermediateX509 = await ParseCaAsync(intermediate);

        Assert.That(rootX509.SigAlgOid, Is.EqualTo(PkcsObjectIdentifiers.Sha384WithRsaEncryption.Id), "RSA-3072 root signs itself with SHA-384");
        Assert.That(intermediateX509.SigAlgOid, Is.EqualTo(PkcsObjectIdentifiers.Sha384WithRsaEncryption.Id), "The RSA root signs the EC intermediate");
        Assert.That(Parse(rsaLeaf).SigAlgOid, Is.EqualTo(X9ObjectIdentifiers.ECDsaWithSha256.Id), "The EC intermediate signs the RSA leaf");
        Assert.That(Parse(ecLeaf).SigAlgOid, Is.EqualTo(X9ObjectIdentifiers.ECDsaWithSha256.Id));

        rootX509.Verify(rootX509.GetPublicKey());
        intermediateX509.Verify(rootX509.GetPublicKey());
        Parse(rsaLeaf).Verify(intermediateX509.GetPublicKey());
        Parse(ecLeaf).Verify(intermediateX509.GetPublicKey());
    }

    [Test]
    public async Task EcChain_GetsStrongerGoingDown_AndSignsWithTheIssuersCurve()
    {
        var root = await CreateCaAsync(Algorithm.EllipticCurve, EllipticCurve.P384, null);
        var intermediate = await CreateCaAsync(Algorithm.EllipticCurve, EllipticCurve.P384, null, root);
        var leaf = await IssueLeafAsync(intermediate, Algorithm.EllipticCurve, EllipticCurve.P521, null);

        var intermediateX509 = await ParseCaAsync(intermediate);

        Assert.That(intermediateX509.SigAlgOid, Is.EqualTo(X9ObjectIdentifiers.ECDsaWithSha384.Id));
        Assert.That(Parse(leaf).SigAlgOid, Is.EqualTo(X9ObjectIdentifiers.ECDsaWithSha384.Id), "Signed by the P-384 issuer, not the P-521 subject");
        Parse(leaf).Verify(intermediateX509.GetPublicKey());
    }

    // Issuer first, subject second; "accepted" is whether a CA and a leaf with the subject's key may be issued
    [TestCase(Algorithm.RSA, null, 2048, Algorithm.RSA, null, 2048, true)]
    [TestCase(Algorithm.RSA, null, 3072, Algorithm.RSA, null, 2048, false)]
    [TestCase(Algorithm.RSA, null, 2048, Algorithm.EllipticCurve, EllipticCurve.P256, null, true)]
    [TestCase(Algorithm.RSA, null, 2048, Algorithm.EllipticCurve, EllipticCurve.P384, null, true)]
    [TestCase(Algorithm.RSA, null, 3072, Algorithm.EllipticCurve, EllipticCurve.P256, null, true)]
    [TestCase(Algorithm.RSA, null, 3072, Algorithm.EllipticCurve, EllipticCurve.P384, null, true)]
    [TestCase(Algorithm.EllipticCurve, EllipticCurve.P256, null, Algorithm.RSA, null, 2048, false)]
    [TestCase(Algorithm.EllipticCurve, EllipticCurve.P256, null, Algorithm.RSA, null, 3072, true)]
    [TestCase(Algorithm.EllipticCurve, EllipticCurve.P384, null, Algorithm.RSA, null, 3072, false)]
    [TestCase(Algorithm.EllipticCurve, EllipticCurve.P256, null, Algorithm.EllipticCurve, EllipticCurve.P256, null, true)]
    [TestCase(Algorithm.EllipticCurve, EllipticCurve.P256, null, Algorithm.EllipticCurve, EllipticCurve.P384, null, true)]
    [TestCase(Algorithm.EllipticCurve, EllipticCurve.P384, null, Algorithm.EllipticCurve, EllipticCurve.P256, null, false)]
    [TestCase(Algorithm.EllipticCurve, EllipticCurve.P521, null, Algorithm.EllipticCurve, EllipticCurve.P384, null, false)]
    public async Task IssuerAndSubjectKeyTypes_AreAcceptedOnlyWhenTheSubjectIsNoWeaker(
        Algorithm issuerAlgorithm, EllipticCurve? issuerCurve, int? issuerLength,
        Algorithm subjectAlgorithm, EllipticCurve? subjectCurve, int? subjectLength,
        bool accepted)
    {
        var issuer = await CreateCaAsync(issuerAlgorithm, issuerCurve, issuerLength);
        var cas = (await _caRepo.GetAllAsync()).Count;
        var certificates = (await _certificateRepo.GetAllAsync()).Count;
        var keys = _keys.Count;

        var childResult = await _authorities.CreateAsync(NewCa(subjectAlgorithm, subjectCurve, subjectLength, issuer), null);
        var leafResult = await TryIssueLeafAsync(issuer, subjectAlgorithm, subjectCurve, subjectLength);

        if (accepted)
        {
            Assert.That(childResult.IsSuccess, Is.True, $"CA: {childResult.Error}");
            Assert.That(leafResult.IsSuccess, Is.True, $"Leaf: {leafResult.Error}");
            return;
        }

        Assert.That(childResult.Status, Is.EqualTo(ResultStatus.Invalid), "CA");
        Assert.That(leafResult.Status, Is.EqualTo(ResultStatus.Invalid), "Leaf");
        Assert.That(childResult.Error, Does.Contain("at least as strong as its issuer"));
        Assert.That(leafResult.Error, Does.Contain("at least as strong as its issuer"));
        Assert.That((await _caRepo.GetAllAsync()).Count, Is.EqualTo(cas), "No CA saved");
        Assert.That((await _certificateRepo.GetAllAsync()).Count, Is.EqualTo(certificates), "No certificate saved");
        Assert.That(_keys.Count, Is.EqualTo(keys), "No key generated");
    }

    private async Task<CertificateAuthority> CreateCaAsync(Algorithm algorithm, EllipticCurve? curve, int? length, CertificateAuthority parent = null)
    {
        var ca = NewCa(algorithm, curve, length, parent);
        var result = await _authorities.CreateAsync(ca, null);
        Assert.That(result.IsSuccess, Is.True, result.Error);
        return ca;
    }

    private async Task<Certificate> IssueLeafAsync(CertificateAuthority issuer, Algorithm algorithm, EllipticCurve? curve, int? length)
    {
        var result = await TryIssueLeafAsync(issuer, algorithm, curve, length);
        Assert.That(result.IsSuccess, Is.True, result.Error);
        return result.Value;
    }

    private async Task<Result<Certificate>> TryIssueLeafAsync(CertificateAuthority issuer, Algorithm algorithm, EllipticCurve? curve, int? length)
    {
        string name = $"leaf-{Guid.NewGuid():N}";

        return await _certificates.IssueCertificateAsync(new CertificateRequest
        {
            CertificateKeyUsageFlags = [(int)CertificateKeyUsage.DigitalSignature],
            Curve = curve,
            ExtendedKeyUsages = [],
            IsCaCertificate = false,
            KeyAlgorithm = algorithm,
            KeySize = length,
            Name = name,
            NotAfter = DateTime.UtcNow.AddYears(1),
            NotBefore = DateTime.UtcNow.AddMinutes(-5),
            ParentCertificateSlug = issuer.CertificateSlug,
            RequestedBySlug = "tester",
            Subject = new DistinguishedName { CommonName = name },
            SubjectAlternativeNames = []
        });
    }

    private static CertificateAuthority NewCa(Algorithm algorithm, EllipticCurve? curve, int? length, CertificateAuthority parent = null)
    {
        string name = $"ca-{Guid.NewGuid():N}";

        return new CertificateAuthority
        {
            Active = true,
            Created = DateTime.UtcNow,
            CreatedBySlug = "tester",
            DistinguishedName = new DistinguishedName { CommonName = name, Country = "US" },
            IsIssuer = true,
            KeyAlgorithm = algorithm,
            KeyCurve = curve,
            KeyLength = length,
            Name = name,
            NotAfter = parent?.NotAfter ?? DateTime.UtcNow.AddYears(10),
            NotBefore = DateTime.UtcNow.AddMinutes(-5),
            ParentCaSlug = parent?.Slug
        };
    }

    private async Task<X509Certificate> ParseCaAsync(CertificateAuthority ca) => Parse(await _certificates.GetAsync(ca.CertificateSlug));

    private static X509Certificate Parse(Certificate certificate) =>
        new X509CertificateParser().ReadCertificate(System.Text.Encoding.UTF8.GetBytes(certificate.Data));
}
