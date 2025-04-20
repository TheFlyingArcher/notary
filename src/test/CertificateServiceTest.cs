using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;

namespace Notary.Test;

public class CertificateServiceTest
{
    private Mock<ICertificateRepository> _certificateRepo;
    private Mock<ICertificateAuthorityService> _caService;

    private Mock<ICertificateService> _service;

    private static Certificate _certificate = CreateCertificateMock();

    private static CertificateRequest _request = MockRequest();

    public CertificateServiceTest()
    {
        _certificateRepo = new Mock<ICertificateRepository>();
        _caService = new Mock<ICertificateAuthorityService>();
        _service = new Mock<ICertificateService>();
    }

    [SetUp]
    public void SetupTest()
    {
        var certificateList = new List<Certificate>
        {
            _certificate
        };
        _certificateRepo.Setup(r => r.SaveAsync(_certificate));
        _certificateRepo.Setup(r => r.GetCertificatesByCaAsync(It.IsAny<string>())).ReturnsAsync(certificateList);

        _caService.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync(MockCa());

        _service.Setup(s => s.IssueCertificateAsync(_request)).ReturnsAsync(_certificate);
    }

    [Test]
    public async Task IssueCertificateAsyncTest()
    {
        var mock = _service.Object;

        var certificate = await mock.IssueCertificateAsync(_request);

        Assert.That(certificate, Is.Not.EqualTo(null));
    }

    private static Certificate CreateCertificateMock()
    {
        return new Certificate
        {
            Active = true,
            IssuingSlug = It.IsAny<string>(),
            CreatedBySlug = It.IsAny<string>(),
            Created = DateTime.MinValue,
            IsCaCertificate = It.IsAny<bool>(),
            Issuer = new DistinguishedName
            {
                CommonName = It.IsAny<string>(),
                Country = It.IsAny<string>(),
                Locale = It.IsAny<string>(),
                Organization = It.IsAny<string>(),
                OrganizationalUnit = It.IsAny<string>(),
                StateProvince = It.IsAny<string>()
            },
            ExtendedKeyUsages = It.IsAny<List<string>>(),
            Name = It.IsAny<string>(),
            NotAfter = DateTime.MaxValue,
            NotBefore = DateTime.MinValue,
            RevocationDate = null,
            SerialNumber = It.IsAny<string>(),
            SignatureAlgorithm = It.IsAny<string>(),
            Slug = It.IsAny<string>(),
            Subject = new DistinguishedName
            {
                CommonName = It.IsAny<string>(),
                Country = It.IsAny<string>(),
                Locale = It.IsAny<string>(),
                Organization = It.IsAny<string>(),
                OrganizationalUnit = It.IsAny<string>(),
                StateProvince = It.IsAny<string>()
            },
            SubjectAlternativeNames = new List<SubjectAlternativeName>(),
            Thumbprint = It.IsAny<string>(),
            Updated = null,
            UpdatedBySlug = null
        };
    }

    private static CertificateAuthority MockCa()
    {
        return new CertificateAuthority
        {
            Active = true,
            Created = DateTime.MinValue,
            CreatedBySlug = It.IsAny<string>(),
            DistinguishedName = new DistinguishedName
            {
                CommonName = It.IsAny<string>(),
                Country = It.IsAny<string>(),
                Locale = It.IsAny<string>(),
                Organization = It.IsAny<string>(),
                OrganizationalUnit = It.IsAny<string>(),
                StateProvince = It.IsAny<string>()
            },
            IsIssuer = It.IsAny<bool>(),
            IssuingDn = new DistinguishedName
            {
                CommonName = It.IsAny<string>(),
                Country = It.IsAny<string>(),
                Locale = It.IsAny<string>(),
                Organization = It.IsAny<string>(),
                OrganizationalUnit = It.IsAny<string>(),
                StateProvince = It.IsAny<string>()
            },
            KeyAlgorithm = It.IsAny<Algorithm>(),
            KeyCurve = null,
            KeyLength = It.IsAny<int>(),
            Name = It.IsAny<string>(),
            ParentCaSlug = It.IsAny<string>(),
            Slug = It.IsAny<string>(),
            Updated = null,
            UpdatedBySlug = It.IsAny<string>()
        };
    }

    private static CertificateRequest MockRequest()
    {
        return new CertificateRequest
        {
            CertificateKeyUsageFlags = It.IsAny<IEnumerable<int>>(),
            CrlEndpoint = It.IsAny<string>(),
            ParentCertificateSlug = It.IsAny<string>(),
            Curve = null,
            KeyAlgorithm = Algorithm.RSA,
            KeySize = It.IsAny<int>(),
            ExtendedKeyUsages = It.IsAny<IEnumerable<string>>(),
            Name = It.IsAny<string>(),
            NotAfter = DateTime.MaxValue,
            NotBefore = DateTime.MinValue,
            RequestedBySlug = It.IsAny<string>(),
            Subject = It.IsAny<DistinguishedName>(),
            SubjectAlternativeNames = It.IsAny<List<SubjectAlternativeName>>()
        };
    }
}