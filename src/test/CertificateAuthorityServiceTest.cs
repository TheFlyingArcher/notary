namespace Notary.Test;

using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;
using Notary.Service;

public class CertificateAuthorityServiceTest : NotaryTest
{
    private Mock<ICertificateAuthorityRepository> _caRepo;
    private Mock<ICertificateService> _certificateService;
    private Mock<ILog> _log;
    private CertificateAuthorityService _service;

    public CertificateAuthorityServiceTest()
    {
        _caRepo = new Mock<ICertificateAuthorityRepository>();
        _certificateService = new Mock<ICertificateService>();
        _log = new Mock<ILog>();
    }

    [SetUp]
    public void Setup()
    {
        // The fixture is one instance, so clear what earlier tests set up and called
        _caRepo.Reset();
        _certificateService.Reset();

        var ca = MockCa();
        var certificate = CreateCertificateMock();
        var config = MockConfiguration();
        var caList = new List<CertificateAuthority>()
        {
            ca
        };
        var certificateList = new List<Certificate>()
        {
            certificate
        };

        _caRepo.Setup(r => r.SaveAsync(ca));
        _caRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(caList);
        _caRepo.Setup(r => r.GetAsync("string")).ReturnsAsync(ca);

        _certificateService.Setup(r => r.GetCertificatesByCaAsync("string")).ReturnsAsync(certificateList);

        _service = new CertificateAuthorityService(
            _caRepo.Object,
            _certificateService.Object,
            config,
            _log.Object
        );
    }

    [Test]
    public async Task GetCaListBriefTest()
    {
        var ca = MockCa();
        var caBrief = new CaBrief
        {
            Certificates = 0,
            CreatedBy = ca.CreatedBySlug,
            CreatedOn = ca.Created.ToString(),
            Name = ca.Name,
            ParentName = "string",
            Slug = ca.Slug
        };

        var expected = new List<CaBrief>() { caBrief };
        var actual = await _service.GetCaListBrief();

        Assert.That(actual, Is.Not.EqualTo(null), "Actual returned null");
        if (actual != null)
        {
            Assert.That(actual.Count, Is.GreaterThan(0), "Actual returned empty");
            Assert.That(actual.Count, Is.EqualTo(expected.Count), "Actual and expected counts differ");
            Assert.That(actual[0].Slug, Is.EqualTo(expected[0].Slug), "Slugs do not match");
        }
    }

    [Test]
    public async Task CreateAsync_IssuanceRejectedForValidity_ReturnsInvalidAndDoesNotSave()
    {
        var parent = MockCa();
        parent.Slug = "parent";
        parent.CertificateSlug = "parent-cert";
        SetKey(parent, Algorithm.EllipticCurve, EllipticCurve.P256, null);
        _caRepo.Setup(r => r.GetAsync("parent")).ReturnsAsync(parent);
        _certificateService
            .Setup(s => s.IssueCertificateAsync(It.IsAny<CertificateRequest>()))
            .ReturnsAsync(Result<Certificate>.Fail(ResultStatus.Invalid, "too long"));

        var child = MockCa();
        child.Slug = "child";
        child.ParentCaSlug = "parent";
        SetKey(child, Algorithm.EllipticCurve, EllipticCurve.P256, null);
        child.DistinguishedName.Country = "US";

        var result = await _service.CreateAsync(child, null);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Is.EqualTo("too long"));
        _caRepo.Verify(r => r.SaveAsync(child), Times.Never);
    }

    [Test]
    public async Task CreateAsync_IssuanceSucceeds_SavesAndReturnsCa()
    {
        var parent = MockCa();
        parent.Slug = "parent";
        parent.CertificateSlug = "parent-cert";
        SetKey(parent, Algorithm.EllipticCurve, EllipticCurve.P256, null);
        _caRepo.Setup(r => r.GetAsync("parent")).ReturnsAsync(parent);
        _certificateService
            .Setup(s => s.IssueCertificateAsync(It.IsAny<CertificateRequest>()))
            .ReturnsAsync(Result<Certificate>.Ok(CreateCertificateMock()));

        var child = MockCa();
        child.Slug = "child";
        child.ParentCaSlug = "parent";
        SetKey(child, Algorithm.EllipticCurve, EllipticCurve.P256, null);
        child.DistinguishedName.Country = "US";

        var result = await _service.CreateAsync(child, null);

        Assert.That(result.IsSuccess, Is.True, result.Error);
        _caRepo.Verify(r => r.SaveAsync(child), Times.Once);
    }

    [TestCase(Algorithm.RSA, null, 3072, Algorithm.EllipticCurve, EllipticCurve.P256, null)]
    [TestCase(Algorithm.EllipticCurve, EllipticCurve.P256, null, Algorithm.RSA, null, 3072)]
    [TestCase(Algorithm.RSA, null, 2048, Algorithm.EllipticCurve, EllipticCurve.P384, null)]
    [TestCase(Algorithm.EllipticCurve, EllipticCurve.P384, null, Algorithm.RSA, null, 7680)]
    [TestCase(Algorithm.EllipticCurve, EllipticCurve.P256, null, Algorithm.EllipticCurve, EllipticCurve.P521, null)]
    [TestCase(Algorithm.RSA, null, 2048, Algorithm.RSA, null, 4096)]
    public async Task CreateAsync_ChildAtLeastAsStrongAsParent_KeepsItsOwnKeySettings(
        Algorithm parentAlgorithm, EllipticCurve? parentCurve, int? parentLength,
        Algorithm childAlgorithm, EllipticCurve? childCurve, int? childLength)
    {
        var parent = MockCa();
        parent.Slug = "parent";
        parent.CertificateSlug = "parent-cert";
        SetKey(parent, parentAlgorithm, parentCurve, parentLength);
        _caRepo.Setup(r => r.GetAsync("parent")).ReturnsAsync(parent);

        CertificateRequest issued = null;
        _certificateService
            .Setup(s => s.IssueCertificateAsync(It.IsAny<CertificateRequest>()))
            .Callback<CertificateRequest>(r => issued = r)
            .ReturnsAsync(Result<Certificate>.Ok(CreateCertificateMock()));

        var child = MockCa();
        child.Slug = "child";
        child.ParentCaSlug = "parent";
        SetKey(child, childAlgorithm, childCurve, childLength);
        child.DistinguishedName.Country = "US";

        var result = await _service.CreateAsync(child, null);

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(child.KeyAlgorithm, Is.EqualTo(childAlgorithm));
        Assert.That(child.KeyCurve, Is.EqualTo(childCurve));
        Assert.That(child.KeyLength, Is.EqualTo(childLength));
        Assert.That(issued.KeyAlgorithm, Is.EqualTo(childAlgorithm));
        Assert.That(issued.Curve, Is.EqualTo(childCurve));
        Assert.That(issued.KeySize, Is.EqualTo(childLength));
        _caRepo.Verify(r => r.SaveAsync(child), Times.Once);
    }

    [TestCase(Algorithm.EllipticCurve, EllipticCurve.P384, null, Algorithm.EllipticCurve, EllipticCurve.P256, null)]
    [TestCase(Algorithm.EllipticCurve, EllipticCurve.P256, null, Algorithm.RSA, null, 2048)]
    [TestCase(Algorithm.RSA, null, 3072, Algorithm.RSA, null, 2048)]
    [TestCase(Algorithm.RSA, null, 7680, Algorithm.EllipticCurve, EllipticCurve.P256, null)]
    public async Task CreateAsync_ChildWeakerThanParent_ReturnsInvalidAndIssuesAndSavesNothing(
        Algorithm parentAlgorithm, EllipticCurve? parentCurve, int? parentLength,
        Algorithm childAlgorithm, EllipticCurve? childCurve, int? childLength)
    {
        var parent = MockCa();
        parent.Slug = "parent";
        parent.CertificateSlug = "parent-cert";
        SetKey(parent, parentAlgorithm, parentCurve, parentLength);
        _caRepo.Setup(r => r.GetAsync("parent")).ReturnsAsync(parent);

        var child = MockCa();
        child.Slug = "child";
        child.ParentCaSlug = "parent";
        SetKey(child, childAlgorithm, childCurve, childLength);
        child.DistinguishedName.Country = "US";

        var result = await _service.CreateAsync(child, null);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("at least as strong as its issuer"));
        _certificateService.Verify(s => s.IssueCertificateAsync(It.IsAny<CertificateRequest>()), Times.Never);
        _caRepo.Verify(r => r.SaveAsync(child), Times.Never);
    }

    [Test]
    public async Task CreateAsync_ParentAlreadyBelowItsOwnIssuer_IsComparedOnlyWithTheDirectParent()
    {
        // An old chain that breaks the rule keeps working: the stored P-256 parent sits under a P-384 CA
        var grandparent = MockCa();
        grandparent.Slug = "grandparent";
        SetKey(grandparent, Algorithm.EllipticCurve, EllipticCurve.P384, null);
        _caRepo.Setup(r => r.GetAsync("grandparent")).ReturnsAsync(grandparent);

        var parent = MockCa();
        parent.Slug = "parent";
        parent.CertificateSlug = "parent-cert";
        parent.ParentCaSlug = "grandparent";
        SetKey(parent, Algorithm.EllipticCurve, EllipticCurve.P256, null);
        _caRepo.Setup(r => r.GetAsync("parent")).ReturnsAsync(parent);

        _certificateService
            .Setup(s => s.IssueCertificateAsync(It.IsAny<CertificateRequest>()))
            .ReturnsAsync(Result<Certificate>.Ok(CreateCertificateMock()));

        var child = MockCa();
        child.Slug = "child";
        child.ParentCaSlug = "parent";
        SetKey(child, Algorithm.EllipticCurve, EllipticCurve.P256, null);
        child.DistinguishedName.Country = "US";

        var loaded = await _service.GetAsync("parent");
        var result = await _service.CreateAsync(child, null);

        Assert.That(loaded, Is.Not.Null, "The existing CA still loads");
        Assert.That(result.IsSuccess, Is.True, result.Error);
        _caRepo.Verify(r => r.GetAsync("grandparent"), Times.Never);
    }

    [Test]
    public async Task CreateAsync_EcChildWithoutACurve_ReturnsInvalid()
    {
        var parent = MockCa();
        parent.Slug = "parent";
        parent.CertificateSlug = "parent-cert";
        SetKey(parent, Algorithm.EllipticCurve, EllipticCurve.P256, null);
        _caRepo.Setup(r => r.GetAsync("parent")).ReturnsAsync(parent);

        var child = MockCa();
        child.Slug = "child";
        child.ParentCaSlug = "parent";
        SetKey(child, Algorithm.EllipticCurve, null, null);
        child.DistinguishedName.Country = "US";

        var result = await _service.CreateAsync(child, null);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        _certificateService.Verify(s => s.IssueCertificateAsync(It.IsAny<CertificateRequest>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_Root_IsNotComparedToAnyIssuer()
    {
        _certificateService
            .Setup(s => s.IssueCertificateAsync(It.IsAny<CertificateRequest>()))
            .ReturnsAsync(Result<Certificate>.Ok(CreateCertificateMock()));

        var root = MockCa();
        root.Slug = "root";
        root.ParentCaSlug = null;
        SetKey(root, Algorithm.RSA, null, 2048);
        root.DistinguishedName.Country = "US";

        var result = await _service.CreateAsync(root, null);

        Assert.That(result.IsSuccess, Is.True, result.Error);
        _caRepo.Verify(r => r.SaveAsync(root), Times.Once);
    }

    private static void SetKey(CertificateAuthority ca, Algorithm algorithm, EllipticCurve? curve, int? length)
    {
        ca.KeyAlgorithm = algorithm;
        ca.KeyCurve = curve;
        ca.KeyLength = length;
    }

    private CertificateAuthority MockCa()
    {
        return new CertificateAuthority
        {
            Active = true,
            Created = DateTime.MinValue,
            CreatedBySlug = "string",
            DistinguishedName = new DistinguishedName
            {
                CommonName = "string",
                Country = "string",
                Locale = "string",
                Organization = "string",
                OrganizationalUnit = "string",
                StateProvince = "string"
            },
            IsIssuer = false,
            IssuingDn = new DistinguishedName
            {
                CommonName = "string",
                Country = "string",
                Locale = "string",
                Organization = "string",
                OrganizationalUnit = "string",
                StateProvince = "string"
            },
            KeyAlgorithm = It.IsAny<Algorithm>(),
            KeyCurve = null,
            KeyLength = 0,
            Name = "string",
            ParentCaSlug = "string",
            Slug = "string",
            Updated = null,
            UpdatedBySlug = "string"
        };
    }

    private Certificate CreateCertificateMock()
    {
        return new Certificate
        {
            Active = true,
            IssuingSlug = "string",
            CreatedBySlug = "string",
            Created = DateTime.MinValue,
            IsCaCertificate = false,
            Issuer = new DistinguishedName
            {
                CommonName = "string",
                Country = "string",
                Locale = "string",
                Organization = "string",
                OrganizationalUnit = "string",
                StateProvince = "string"
            },
            ExtendedKeyUsages = new List<string>(),
            Name = "string",
            NotAfter = DateTime.MaxValue,
            NotBefore = DateTime.MinValue,
            RevocationDate = null,
            SerialNumber = "string",
            SignatureAlgorithm = "string",
            Slug = "string",
            Subject = new DistinguishedName
            {
                CommonName = "string",
                Country = "string",
                Locale = "string",
                Organization = "string",
                OrganizationalUnit = "string",
                StateProvince = "string"
            },
            SubjectAlternativeNames = new List<SubjectAlternativeName>(),
            Thumbprint = "string",
            Updated = null,
            UpdatedBySlug = null
        };
    }
}
