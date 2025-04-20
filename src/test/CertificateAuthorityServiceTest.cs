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
