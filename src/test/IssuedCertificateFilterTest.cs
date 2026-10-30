using Notary.Contract;

namespace Notary.Test;

public class IssuedCertificateFilterTest
{
    private static Certificate Cert(string name, bool isCa)
    {
        return new Certificate { Slug = name.ToLowerInvariant(), Name = name, IsCaCertificate = isCa };
    }

    private static readonly Certificate[] Issued =
    [
        Cert("web", false),
        Cert("Sub CA", true),
        Cert("api", false)
    ];

    [Test]
    public void ForCa_RootCa_KeepsChildCasAndLeaves()
    {
        var result = IssuedCertificateFilter.ForCa(Issued, isRootCa: true);

        Assert.That(result.Select(c => c.Name), Is.EqualTo(new[] { "api", "Sub CA", "web" }));
    }

    [Test]
    public void ForCa_NonRootCa_ListsLeafCertificatesOnly()
    {
        var result = IssuedCertificateFilter.ForCa(Issued, isRootCa: false);

        Assert.That(result.Select(c => c.Name), Is.EqualTo(new[] { "api", "web" }));
    }

    [Test]
    public void ForCa_NothingIssued_IsEmpty()
    {
        Assert.That(IssuedCertificateFilter.ForCa([], isRootCa: true), Is.Empty);
    }
}
