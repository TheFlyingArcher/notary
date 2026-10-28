using Notary.Download;

namespace Notary.Test;

public class CertificateDownloadCatalogTest
{
    [Test]
    public void All_CoversEveryCertificateFormat()
    {
        var formats = CertificateDownloadCatalog.All.Select(o => o.Format).ToList();

        Assert.That(formats, Is.EquivalentTo(Enum.GetValues<CertificateFormat>()));
    }

    [Test]
    public void All_HasTheFriendlyLabelsInDialogOrder()
    {
        var labels = CertificateDownloadCatalog.All.Select(o => o.Label).ToList();

        Assert.That(labels, Is.EqualTo(new[]
        {
            "Certificate only (.cer)",
            "Certificate only, text (.pem)",
            "Certificate + private key bundle (.pfx)",
            "Certificate chain, text (.p7b)",
            "Certificate chain, binary (.p7b)"
        }));
    }

    [TestCaseSource(nameof(AllFormats))]
    public void Option_HasLabelExtensionAndBlurb(CertificateFormat format)
    {
        var option = CertificateDownloadCatalog.For(format);

        Assert.Multiple(() =>
        {
            Assert.That(option.Format, Is.EqualTo(format));
            Assert.That(option.Label, Is.Not.Empty);
            Assert.That(option.Extension, Does.StartWith("."));
            Assert.That(option.Blurb, Is.Not.Empty);
        });
    }

    [TestCaseSource(nameof(AllFormats))]
    public void Option_OnlyPkcs12RequiresPasswordAndContainsPrivateKey(CertificateFormat format)
    {
        var option = CertificateDownloadCatalog.For(format);
        var isBundle = format == CertificateFormat.Pkcs12;

        Assert.Multiple(() =>
        {
            Assert.That(option.RequiresPassword, Is.EqualTo(isBundle));
            Assert.That(option.ContainsPrivateKey, Is.EqualTo(isBundle));
        });
    }

    [TestCaseSource(nameof(AllFormats))]
    public void Blurb_MentionsPrivateKeyOnlyWhenItIsIncluded(CertificateFormat format)
    {
        var option = CertificateDownloadCatalog.For(format);

        if (option.ContainsPrivateKey)
        {
            Assert.That(option.Blurb, Does.Contain("password-protected").And.Contain("includes the private key"));
        }
        else
        {
            Assert.That(option.Blurb, Does.Contain("does not contain the private key"));
        }
    }

    [Test]
    public void ForCertificate_ALeaf_GetsEveryOption()
    {
        Assert.That(CertificateDownloadCatalog.ForCertificate(false), Is.EqualTo(CertificateDownloadCatalog.All));
    }

    [Test]
    public void ForCertificate_ACa_GetsNoOptionThatContainsAPrivateKey()
    {
        var options = CertificateDownloadCatalog.ForCertificate(true);

        Assert.Multiple(() =>
        {
            Assert.That(options, Is.Not.Empty);
            Assert.That(options.Any(o => o.ContainsPrivateKey), Is.False);
            Assert.That(options.Select(o => o.Format), Does.Not.Contain(CertificateFormat.Pkcs12));
            Assert.That(options.Count, Is.EqualTo(CertificateDownloadCatalog.All.Count - 1));
        });
    }

    [Test]
    public void For_UndefinedFormat_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CertificateDownloadCatalog.For((CertificateFormat)999));
    }

    [TestCase(CertificateFormat.Der, ".cer")]
    [TestCase(CertificateFormat.Pem, ".pem")]
    [TestCase(CertificateFormat.Pkcs12, ".pfx")]
    [TestCase(CertificateFormat.Pkcs7, ".p7b")]
    [TestCase(CertificateFormat.Pkcs7Pem, ".p7b")]
    public void BuildFileName_BlankInput_UsesCertificateNameAndExtension(CertificateFormat format, string extension)
    {
        Assert.Multiple(() =>
        {
            Assert.That(CertificateDownloadCatalog.BuildFileName(format, null, "web-server"), Is.EqualTo("web-server" + extension));
            Assert.That(CertificateDownloadCatalog.BuildFileName(format, "", "web-server"), Is.EqualTo("web-server" + extension));
            Assert.That(CertificateDownloadCatalog.BuildFileName(format, "   ", "web-server"), Is.EqualTo("web-server" + extension));
        });
    }

    [Test]
    public void BuildFileName_UserName_GetsExtensionForFormat()
    {
        var name = CertificateDownloadCatalog.BuildFileName(CertificateFormat.Pkcs12, "my-bundle", "web-server");

        Assert.That(name, Is.EqualTo("my-bundle.pfx"));
    }

    [TestCase("my-cert.pem")]
    [TestCase("my-cert.PEM")]
    [TestCase("my-cert.pem ")]
    public void BuildFileName_UserTypedExtension_IsNotDoubled(string typed)
    {
        var name = CertificateDownloadCatalog.BuildFileName(CertificateFormat.Pem, typed, "web-server");

        Assert.That(name, Is.EqualTo("my-cert.pem"));
    }

    [Test]
    public void BuildFileName_DifferentExtensionTyped_KeepsItInTheName()
    {
        var name = CertificateDownloadCatalog.BuildFileName(CertificateFormat.Pem, "my-cert.v2", "web-server");

        Assert.That(name, Is.EqualTo("my-cert.v2.pem"));
    }

    [Test]
    public void BuildFileName_InvalidAndPathCharacters_AreStripped()
    {
        var name = CertificateDownloadCatalog.BuildFileName(CertificateFormat.Der, "..\\..\\evil/na<me>:?*\"|", "web-server");

        Assert.Multiple(() =>
        {
            Assert.That(name, Is.EqualTo("evilname.cer"));
            Assert.That(name.IndexOfAny(Path.GetInvalidFileNameChars()), Is.EqualTo(-1));
        });
    }

    [Test]
    public void BuildFileName_NothingLeftAfterStripping_FallsBackToCertificateName()
    {
        var name = CertificateDownloadCatalog.BuildFileName(CertificateFormat.Der, "<>:?*", "web-server");

        Assert.That(name, Is.EqualTo("web-server.cer"));
    }

    [Test]
    public void BuildFileName_CertificateNameIsAlsoSanitized()
    {
        var name = CertificateDownloadCatalog.BuildFileName(CertificateFormat.Pem, null, "a/b:c");

        Assert.That(name, Is.EqualTo("abc.pem"));
    }

    [Test]
    public void BuildFileName_NoUsableNameAnywhere_UsesGenericName()
    {
        var name = CertificateDownloadCatalog.BuildFileName(CertificateFormat.Pkcs7, "", "");

        Assert.That(name, Is.EqualTo("certificate.p7b"));
    }

    private static IEnumerable<CertificateFormat> AllFormats()
    {
        return Enum.GetValues<CertificateFormat>();
    }
}
