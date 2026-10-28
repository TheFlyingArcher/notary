using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;

using Notary.Configuration;
using Notary.Contract;
using Notary.Interface.Service;
using Notary.Profiles;
using Notary.Service;

using Org.BouncyCastle.Cms;

namespace Notary.Test;

/// <summary>
/// Exports real certificates (root, intermediate, leaf) over in-memory storage and parses what comes out.
/// </summary>
public class CertificateExportTest
{
    private const string Password = "Export-Passw0rd!";

    private FakeKeyService _keys;
    private FakeCertificateRepository _certificateRepo;
    private FakeCertificateAuthorityRepository _caRepo;
    private CertificateService _certificates;
    private CertificateAuthorityService _service;

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
        _service = new CertificateAuthorityService(_caRepo, _certificates, config, log);
    }

    private record Chain(Certificate Root, Certificate Intermediate, Certificate Leaf);

    private async Task<Chain> IssueChainAsync(Algorithm algorithm)
    {
        var root = NewCa(CaPurpose.Root, algorithm, null);
        Assert.That((await _service.CreateAsync(root, null)).IsSuccess, Is.True);
        var intermediate = NewCa(CaPurpose.TlsSsl, algorithm, root);
        var created = await _service.CreateAsync(intermediate, null);
        Assert.That(created.IsSuccess, Is.True, created.Error);

        var leaf = await _certificates.IssueCertificateAsync(new CertificateRequest
        {
            CertificateKeyUsageFlags = [(int)CertificateKeyUsage.DigitalSignature],
            Curve = EllipticCurve.P384,
            ExtendedKeyUsages = [KeyPurposeOids.ServerAuthentication],
            IsCaCertificate = false,
            KeyAlgorithm = algorithm,
            KeySize = algorithm == Algorithm.RSA ? 2048 : null,
            Name = "leaf",
            NotAfter = intermediate.NotAfter,
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            ParentCertificateSlug = intermediate.CertificateSlug,
            RequestedBySlug = "tester",
            Subject = new DistinguishedName { CommonName = "leaf.example", Country = "US" },
            SubjectAlternativeNames = []
        });
        Assert.That(leaf.IsSuccess, Is.True, leaf.Error);

        return new Chain(
            await _certificates.GetAsync(root.CertificateSlug),
            await _certificates.GetAsync(intermediate.CertificateSlug),
            leaf.Value);
    }

    private static CertificateAuthority NewCa(CaPurpose purpose, Algorithm algorithm, CertificateAuthority parent)
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
            KeyCurve = algorithm == Algorithm.EllipticCurve ? EllipticCurve.P384 : null,
            KeyLength = algorithm == Algorithm.RSA ? 2048 : null,
            Name = name,
            NotAfter = parent?.NotAfter ?? DateTime.UtcNow.AddYears(10),
            NotBefore = DateTime.UtcNow.AddMinutes(-5),
            ParentCaSlug = parent?.Slug,
            Purpose = purpose
        };
    }

    private async Task<byte[]> ExportPkcs12Async(Certificate certificate, string? password = Password) =>
        await _certificates.RequestCertificateAsync(certificate.Slug, CertificateFormat.Pkcs12, password);

    // The certificates in a PKCS #12, whatever the safe contents holding them
    private static List<System.Security.Cryptography.X509Certificates.X509Certificate2> ReadCertificates(byte[] pfx, string password)
    {
        var info = Pkcs12Info.Decode(pfx, out _);
        List<System.Security.Cryptography.X509Certificates.X509Certificate2> certificates = [];
        foreach (var contents in info.AuthenticatedSafe)
        {
            if (contents.ConfidentialityMode == Pkcs12ConfidentialityMode.Password)
                contents.Decrypt(password);

            foreach (var bag in contents.GetBags().OfType<Pkcs12CertBag>())
                certificates.Add(bag.GetCertificate());
        }

        return certificates;
    }

    private static Pkcs12ShroudedKeyBag ReadKeyBag(byte[] pfx)
    {
        var info = Pkcs12Info.Decode(pfx, out _);
        // The certificates sit in password-encrypted contents, which cannot be enumerated without decrypting
        var bags = info.AuthenticatedSafe
            .Where(contents => contents.ConfidentialityMode == Pkcs12ConfidentialityMode.None)
            .SelectMany(contents => contents.GetBags())
            .OfType<Pkcs12ShroudedKeyBag>()
            .ToList();
        Assert.That(bags, Has.Count.EqualTo(1), "exactly one private key");
        return bags[0];
    }

    [TestCase(Algorithm.EllipticCurve)]
    [TestCase(Algorithm.RSA)]
    public async Task Pkcs12_ContainsTheLeafKeyAndTheWholeChain(Algorithm algorithm)
    {
        var chain = await IssueChainAsync(algorithm);

        var pfx = await ExportPkcs12Async(chain.Leaf);

        var info = Pkcs12Info.Decode(pfx, out _);
        Assert.That(info.VerifyMac(Password), Is.True);
        var certificates = ReadCertificates(pfx, Password);
        Assert.That(certificates, Has.Count.EqualTo(3));
        Assert.That(certificates[0].Subject, Does.Contain("leaf.example"), "the leaf comes first");
        Assert.That(certificates[1].Subject, Is.EqualTo(certificates[0].Issuer), "then its issuer");
        Assert.That(certificates[2].Subject, Is.EqualTo(certificates[1].Issuer), "then the root");
        Assert.That(certificates[2].Subject, Is.EqualTo(certificates[2].Issuer), "which is self-signed");
    }

    [TestCase(Algorithm.EllipticCurve)]
    [TestCase(Algorithm.RSA)]
    public async Task Pkcs12_PrivateKeyMatchesTheLeafCertificate(Algorithm algorithm)
    {
        var chain = await IssueChainAsync(algorithm);
        var pfx = await ExportPkcs12Async(chain.Leaf);

        var leaf = ReadCertificates(pfx, Password)[0];
        var keyInfo = Pkcs8PrivateKeyInfo.DecryptAndDecode(Password, ReadKeyBag(pfx).EncryptedPkcs8PrivateKey, out _);

        byte[] publicKeyFromKey;
        if (algorithm == Algorithm.RSA)
        {
            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(keyInfo.Encode(), out _);
            publicKeyFromKey = rsa.ExportSubjectPublicKeyInfo();
        }
        else
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportPkcs8PrivateKey(keyInfo.Encode(), out _);
            publicKeyFromKey = ecdsa.ExportSubjectPublicKeyInfo();
        }

        Assert.That(publicKeyFromKey, Is.EqualTo(leaf.PublicKey.ExportSubjectPublicKeyInfo()));
    }

    [Test]
    public async Task Pkcs12_WrongPassword_CannotOpenTheFile()
    {
        var chain = await IssueChainAsync(Algorithm.EllipticCurve);
        var pfx = await ExportPkcs12Async(chain.Leaf);

        var info = Pkcs12Info.Decode(pfx, out _);
        Assert.That(info.VerifyMac("Wrong-Passw0rd!"), Is.False);
        Assert.That(() => ReadCertificates(pfx, "Wrong-Passw0rd!"), Throws.InstanceOf<CryptographicException>());
        Assert.That(
            () => Pkcs8PrivateKeyInfo.DecryptAndDecode("Wrong-Passw0rd!", ReadKeyBag(pfx).EncryptedPkcs8PrivateKey, out _),
            Throws.InstanceOf<CryptographicException>());
    }

    [Test]
    public async Task Pkcs12_UsesAes256Pbkdf2Sha256AndASha256Mac()
    {
        var chain = await IssueChainAsync(Algorithm.EllipticCurve);
        var pfx = await ExportPkcs12Async(chain.Leaf);

        // PFX ::= SEQUENCE { version, authSafe ContentInfo, macData MacData }
        var pfxReader = new AsnReader(pfx, AsnEncodingRules.BER).ReadSequence();
        pfxReader.ReadInteger();
        var authSafe = pfxReader.ReadSequence();
        authSafe.ReadObjectIdentifier();
        var authSafeContent = new AsnReader(authSafe.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0)).ReadOctetString(), AsnEncodingRules.BER);

        // MacData ::= SEQUENCE { mac DigestInfo, macSalt, iterations }
        var macData = pfxReader.ReadSequence();
        var digestInfo = macData.ReadSequence();
        var digestAlgorithm = digestInfo.ReadSequence();
        Assert.That(digestAlgorithm.ReadObjectIdentifier(), Is.EqualTo("2.16.840.1.101.3.4.2.1"), "SHA-256 MAC");
        macData.ReadOctetString();
        Assert.That((int)macData.ReadInteger(), Is.GreaterThanOrEqualTo(600_000), "MAC iterations");

        // The certificates are in a password-encrypted ContentInfo; the key is a shrouded key bag
        var contents = authSafeContent.ReadSequence();
        bool sawEncryptedCertificates = false;
        while (contents.HasData)
        {
            var contentInfo = contents.ReadSequence();
            string contentType = contentInfo.ReadObjectIdentifier();
            if (contentType == "1.2.840.113549.1.7.6")
            {
                var encryptedData = contentInfo.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0)).ReadSequence();
                encryptedData.ReadInteger();
                var encryptedContentInfo = encryptedData.ReadSequence();
                encryptedContentInfo.ReadObjectIdentifier();
                AssertPbes2WithAes256(encryptedContentInfo.ReadEncodedValue().ToArray(), "certificates");
                sawEncryptedCertificates = true;
            }
        }

        Assert.That(sawEncryptedCertificates, Is.True);
        AssertPbes2WithAes256(ReadKeyBag(pfx).EncryptedPkcs8PrivateKey.ToArray(), "private key", isEncryptedPrivateKeyInfo: true);
    }

    // Checks the AlgorithmIdentifier is PBES2 with PBKDF2 (HMAC-SHA256, >= 600,000 iterations) and AES-256-CBC
    private static void AssertPbes2WithAes256(byte[] encoded, string what, bool isEncryptedPrivateKeyInfo = false)
    {
        // EncryptedPrivateKeyInfo and EncryptedContentInfo (after its content type) both start with the algorithm
        var reader = new AsnReader(encoded, AsnEncodingRules.BER);
        var algorithm = isEncryptedPrivateKeyInfo ? reader.ReadSequence().ReadSequence() : reader.ReadSequence();

        Assert.That(algorithm.ReadObjectIdentifier(), Is.EqualTo("1.2.840.113549.1.5.13"), $"{what}: PBES2");
        var pbes2 = algorithm.ReadSequence();
        var kdf = pbes2.ReadSequence();
        Assert.That(kdf.ReadObjectIdentifier(), Is.EqualTo("1.2.840.113549.1.5.12"), $"{what}: PBKDF2");
        var pbkdf2 = kdf.ReadSequence();
        pbkdf2.ReadOctetString();
        Assert.That((int)pbkdf2.ReadInteger(), Is.GreaterThanOrEqualTo(600_000), $"{what}: iterations");
        if (pbkdf2.PeekTag().TagValue == (int)UniversalTagNumber.Integer)
            pbkdf2.ReadInteger();
        var prf = pbkdf2.ReadSequence();
        Assert.That(prf.ReadObjectIdentifier(), Is.EqualTo("1.2.840.113549.2.9"), $"{what}: HMAC-SHA256");
        var scheme = pbes2.ReadSequence();
        Assert.That(scheme.ReadObjectIdentifier(), Is.EqualTo("2.16.840.1.101.3.4.1.42"), $"{what}: AES-256-CBC");
    }

    [TestCase(Algorithm.EllipticCurve)]
    [TestCase(Algorithm.RSA)]
    public async Task Pkcs12_OfASelfSignedCertificate_ContainsOnlyThatCertificate(Algorithm algorithm)
    {
        var chain = await IssueChainAsync(algorithm);

        var pfx = await ExportPkcs12Async(chain.Root);

        Assert.That(ReadCertificates(pfx, Password), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Pkcs12_WithAMissingIssuer_FailsClearly()
    {
        var chain = await IssueChainAsync(Algorithm.EllipticCurve);
        var leaf = await _certificates.GetAsync(chain.Leaf.Slug);
        leaf.IssuingSlug = "no-such-certificate";
        await _certificateRepo.SaveAsync(leaf);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await ExportPkcs12Async(leaf));

        Assert.That(ex!.Message, Does.Contain("no-such-certificate").And.Contain("not found"));
    }

    [TestCase(null)]
    [TestCase("")]
    public async Task Pkcs12_WithoutAPassword_IsRefused(string? password)
    {
        var chain = await IssueChainAsync(Algorithm.EllipticCurve);

        Assert.ThrowsAsync<ArgumentException>(async () => await ExportPkcs12Async(chain.Leaf, password));
    }

    [TestCase(Algorithm.EllipticCurve)]
    [TestCase(Algorithm.RSA)]
    public async Task Pkcs7_ContainsTheLeafAndItsChain_AndNoSignersOrKeys(Algorithm algorithm)
    {
        var chain = await IssueChainAsync(algorithm);

        var p7b = await _certificates.RequestCertificateAsync(chain.Leaf.Slug, CertificateFormat.Pkcs7, null);

        var signedData = new CmsSignedData(p7b);
        var subjects = signedData.GetCertificates().EnumerateMatches(null).Select(c => c.SubjectDN.ToString()).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(subjects, Has.Count.EqualTo(3));
            Assert.That(subjects, Has.Some.Contain("leaf.example"));
            Assert.That(subjects, Has.Some.Contain(chain.Intermediate.Subject.CommonName));
            Assert.That(subjects, Has.Some.Contain(chain.Root.Subject.CommonName));
            Assert.That(signedData.GetSignerInfos().GetSigners(), Is.Empty);
            Assert.That(signedData.SignedContent, Is.Null);
            Assert.That(p7b, Is.Not.Empty);
        });
    }

    [Test]
    public async Task Pkcs7_OfASelfSignedCertificate_ContainsOnlyThatCertificate()
    {
        var chain = await IssueChainAsync(Algorithm.EllipticCurve);

        var p7b = await _certificates.RequestCertificateAsync(chain.Root.Slug, CertificateFormat.Pkcs7, null);

        Assert.That(new CmsSignedData(p7b).GetCertificates().EnumerateMatches(null).Count(), Is.EqualTo(1));
    }

    [TestCase(Algorithm.EllipticCurve)]
    [TestCase(Algorithm.RSA)]
    public async Task Pkcs7Pem_IsPemArmoredAndDecodesToTheDerExport(Algorithm algorithm)
    {
        var chain = await IssueChainAsync(algorithm);

        var der = await _certificates.RequestCertificateAsync(chain.Leaf.Slug, CertificateFormat.Pkcs7, null);
        var pem = await _certificates.RequestCertificateAsync(chain.Leaf.Slug, CertificateFormat.Pkcs7Pem, null);

        var text = System.Text.Encoding.ASCII.GetString(pem);
        var fields = PemEncoding.Find(text);
        var signedData = new CmsSignedData(Convert.FromBase64String(text[fields.Base64Data]));
        var subjects = signedData.GetCertificates().EnumerateMatches(null).Select(c => c.SubjectDN.ToString()).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(text, Does.StartWith("-----BEGIN PKCS7-----\n"));
            Assert.That(text, Does.EndWith("-----END PKCS7-----\n"));
            Assert.That(text, Does.Not.Contain("\r"));
            Assert.That(text[fields.Label], Is.EqualTo("PKCS7"));
            Assert.That(Convert.FromBase64String(text[fields.Base64Data]), Is.EqualTo(der));
            Assert.That(subjects, Has.Count.EqualTo(3));
            Assert.That(subjects, Has.Some.Contain("leaf.example"));
            Assert.That(subjects, Has.Some.Contain(chain.Intermediate.Subject.CommonName));
            Assert.That(subjects, Has.Some.Contain(chain.Root.Subject.CommonName));
            Assert.That(signedData.GetSignerInfos().GetSigners(), Is.Empty);
        });
    }

    [Test]
    public async Task Pkcs7Pem_OfASelfSignedCertificate_ContainsOnlyThatCertificate()
    {
        var chain = await IssueChainAsync(Algorithm.EllipticCurve);

        var pem = await _certificates.RequestCertificateAsync(chain.Root.Slug, CertificateFormat.Pkcs7Pem, null);

        var text = System.Text.Encoding.ASCII.GetString(pem);
        var signedData = new CmsSignedData(Convert.FromBase64String(text[PemEncoding.Find(text).Base64Data]));
        Assert.That(signedData.GetCertificates().EnumerateMatches(null).Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task Pkcs7Pem_OfAMissingCertificate_ReturnsNull()
    {
        Assert.That(await _certificates.RequestCertificateAsync("no-such-certificate", CertificateFormat.Pkcs7Pem, null), Is.Null);
    }

    [Test]
    public async Task Resolver_ALoopInTheIssuerLinks_Throws()
    {
        var chain = await IssueChainAsync(Algorithm.EllipticCurve);
        var root = await _certificates.GetAsync(chain.Root.Slug);
        root.IssuingSlug = chain.Leaf.Slug;
        await _certificateRepo.SaveAsync(root);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await CertificateChainResolver.ResolveAsync(chain.Leaf, _certificates.GetAsync));

        Assert.That(ex!.Message, Does.Contain("loops"));
    }

    [Test]
    public void Resolver_AnOverlongChain_Throws()
    {
        Task<Certificate> Next(string slug) =>
            Task.FromResult(new Certificate { Slug = slug + "x", IssuingSlug = slug + "xx" });

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await CertificateChainResolver.ResolveAsync(new Certificate { Slug = "a", IssuingSlug = "b" }, Next));

        Assert.That(ex!.Message, Does.Contain("longer than"));
    }

    private static bool OpenSslAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("openssl", "version") { RedirectStandardOutput = true, UseShellExecute = false });
            process!.WaitForExit(10_000);
            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static (int ExitCode, string Output) RunOpenSsl(params string[] args)
    {
        var startInfo = new ProcessStartInfo("openssl")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);

        using var process = Process.Start(startInfo)!;
        string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit(30_000);
        return (process.ExitCode, output);
    }

    [TestCase(Algorithm.EllipticCurve)]
    [TestCase(Algorithm.RSA)]
    public async Task OpenSsl_OpensThePkcs12WithoutLegacyMode(Algorithm algorithm)
    {
        Assume.That(OpenSslAvailable(), "openssl is not on the path");
        var chain = await IssueChainAsync(algorithm);
        var path = Path.Combine(Path.GetTempPath(), $"export-{Guid.NewGuid():N}.pfx");
        try
        {
            await File.WriteAllBytesAsync(path, await ExportPkcs12Async(chain.Leaf));

            var (exitCode, output) = RunOpenSsl("pkcs12", "-in", path, "-passin", $"pass:{Password}", "-info", "-nodes");

            Assert.That(exitCode, Is.EqualTo(0), output);
            Assert.That(output, Does.Contain("PRIVATE KEY"));
            Assert.That(output.Split("BEGIN CERTIFICATE").Length - 1, Is.EqualTo(3), "leaf, intermediate and root");
            Assert.That(output, Does.Contain("MAC: sha256"));
            Assert.That(output, Does.Not.Contain("RC2").And.Not.Contain("3-KeyTripleDES"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task OpenSsl_ReadsThePkcs7Chain()
    {
        Assume.That(OpenSslAvailable(), "openssl is not on the path");
        var chain = await IssueChainAsync(Algorithm.EllipticCurve);
        var path = Path.Combine(Path.GetTempPath(), $"export-{Guid.NewGuid():N}.p7b");
        try
        {
            await File.WriteAllBytesAsync(path, await _certificates.RequestCertificateAsync(chain.Leaf.Slug, CertificateFormat.Pkcs7, null));

            var (exitCode, output) = RunOpenSsl("pkcs7", "-inform", "DER", "-in", path, "-print_certs");

            Assert.That(exitCode, Is.EqualTo(0), output);
            Assert.That(output.Split("subject=").Length - 1, Is.EqualTo(3));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task OpenSsl_ReadsThePkcs7PemChain_WithoutInform()
    {
        Assume.That(OpenSslAvailable(), "openssl is not on the path");
        var chain = await IssueChainAsync(Algorithm.EllipticCurve);
        var path = Path.Combine(Path.GetTempPath(), $"export-{Guid.NewGuid():N}.p7b");
        try
        {
            await File.WriteAllBytesAsync(path, await _certificates.RequestCertificateAsync(chain.Leaf.Slug, CertificateFormat.Pkcs7Pem, null));

            var (parseExit, parseOutput) = RunOpenSsl("pkcs7", "-in", path, "-noout");
            var (exitCode, output) = RunOpenSsl("pkcs7", "-in", path, "-print_certs");

            Assert.That(parseExit, Is.EqualTo(0), parseOutput);
            Assert.That(exitCode, Is.EqualTo(0), output);
            Assert.That(output.Split("subject=").Length - 1, Is.EqualTo(3));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
