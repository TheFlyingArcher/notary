using Notary.Configuration;
using Notary.Contract;
using Notary.Interface.Service;
using Notary.Profiles;
using Notary.Service;

namespace Notary.Test;

/// <summary>
/// The queries behind the Certificates page: leaf-issuing CAs, certificates per issuing CA, and self-signed certificates.
/// </summary>
public class CertificateListingTest
{
    private FakeCertificateRepository _certificateRepo;
    private FakeCertificateAuthorityRepository _caRepo;
    private Mock<ICertificateService> _certificateService;
    private CertificateAuthorityService _caService;

    [SetUp]
    public void SetUp()
    {
        _certificateRepo = new FakeCertificateRepository();
        _caRepo = new FakeCertificateAuthorityRepository();
        _certificateService = new Mock<ICertificateService>();
        _caService = new CertificateAuthorityService(_caRepo, _certificateService.Object, new NotaryConfiguration(), new Mock<ILog>().Object);
    }

    [Test]
    public async Task LeafIssuingAuthorities_ExcludesRootsAndNonIssuers()
    {
        await AddCaAsync("tls", "Tls", CaPurpose.TlsSsl);
        await AddCaAsync("root", "Root", CaPurpose.Root);
        await AddCaAsync("leaf-only", "Not An Issuer", CaPurpose.TlsSsl, isIssuer: false);

        var result = await _caService.GetLeafIssuingAuthoritiesAsync();

        Assert.That(result.Select(ca => ca.Slug), Is.EqualTo(new[] { "tls" }));
    }

    [Test]
    public async Task LeafIssuingAuthorities_ExcludesRevokedAndMissingCaCertificates()
    {
        await AddCaAsync("good", "Good", CaPurpose.TlsSsl);
        await AddCaAsync("revoked", "Revoked", CaPurpose.TlsSsl, revoked: true);
        await _caRepo.SaveAsync(new CertificateAuthority { Slug = "orphan", Name = "Orphan", IsIssuer = true, Purpose = CaPurpose.TlsSsl, CertificateSlug = "orphan-cert" });

        var result = await _caService.GetLeafIssuingAuthoritiesAsync();

        Assert.That(result.Select(ca => ca.Slug), Is.EqualTo(new[] { "good" }));
    }

    [Test]
    public async Task LeafIssuingAuthorities_AreOrderedByName()
    {
        await AddCaAsync("c", "charlie", CaPurpose.TlsSsl);
        await AddCaAsync("a", "Alpha", CaPurpose.Smime);
        await AddCaAsync("b", "Bravo", CaPurpose.IoT);

        var result = await _caService.GetLeafIssuingAuthoritiesAsync();

        Assert.That(result.Select(ca => ca.Name), Is.EqualTo(new[] { "Alpha", "Bravo", "charlie" }));
    }

    [Test]
    public async Task LeafIssuingAuthorities_WithNoCas_IsEmpty()
    {
        Assert.That(await _caService.GetLeafIssuingAuthoritiesAsync(), Is.Empty);
    }

    [Test]
    public async Task CertificatesByCa_ReturnsOnlyThatIssuersCertificates()
    {
        await _certificateRepo.SaveAsync(Leaf("one", issuedBy: "ca-a-cert"));
        await _certificateRepo.SaveAsync(Leaf("two", issuedBy: "ca-b-cert"));
        await _certificateRepo.SaveAsync(Leaf("self", issuedBy: null));

        var result = await _certificateRepo.GetCertificatesByCaAsync("ca-a-cert");

        Assert.That(result.Select(c => c.Slug), Is.EqualTo(new[] { "one" }));
    }

    [Test]
    public async Task SelfSigned_ReturnsOnlyNonCaCertificatesWithoutAnIssuer()
    {
        await _certificateRepo.SaveAsync(Leaf("issued", issuedBy: "ca-a-cert"));
        await _certificateRepo.SaveAsync(Leaf("self-null", issuedBy: null));
        await _certificateRepo.SaveAsync(Leaf("self-empty", issuedBy: string.Empty));
        await _certificateRepo.SaveAsync(Leaf("root-ca", issuedBy: null, isCa: true));

        var result = await _certificateRepo.GetSelfSignedAsync();

        Assert.That(result.Select(c => c.Slug), Is.EquivalentTo(new[] { "self-null", "self-empty" }));
    }

    [Test]
    public async Task SelfSigned_WithNoCertificates_IsEmpty()
    {
        Assert.That(await _certificateRepo.GetSelfSignedAsync(), Is.Empty);
    }

    private async Task AddCaAsync(string slug, string name, CaPurpose purpose, bool isIssuer = true, bool revoked = false)
    {
        var certificateSlug = slug + "-cert";
        await _caRepo.SaveAsync(new CertificateAuthority
        {
            Slug = slug,
            Name = name,
            IsIssuer = isIssuer,
            Purpose = purpose,
            CertificateSlug = certificateSlug
        });
        _certificateService.Setup(s => s.GetAsync(certificateSlug)).ReturnsAsync(new Certificate
        {
            Slug = certificateSlug,
            IsCaCertificate = true,
            RevocationDate = revoked ? DateTime.UtcNow : null
        });
    }

    private static Certificate Leaf(string slug, string issuedBy, bool isCa = false) => new()
    {
        Slug = slug,
        Name = slug,
        IssuingSlug = issuedBy,
        IsCaCertificate = isCa
    };
}
