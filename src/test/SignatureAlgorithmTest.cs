using Notary.Configuration;
using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;
using Notary.Service;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.X509;

namespace Notary.Test;

/// <summary>
/// The signature algorithm follows the issuer's key: its curve for EC and its size for RSA, whatever the subject's key is.
/// </summary>
public class SignatureAlgorithmTest
{
    private const string CrlEndpoint = "http://pki.test/api/crl";

    private CertificateService _certificates;
    private CertificateRevokeService _revocations;

    [SetUp]
    public void SetUp()
    {
        var config = new NotaryConfiguration();
        var baseUrlProvider = new Mock<ICrlBaseUrlProvider>();
        baseUrlProvider.Setup(p => p.GetCrlBaseUrl()).Returns(CrlEndpoint);
        var log = new Mock<ILog>().Object;

        var keys = new FakeKeyService();
        _certificates = new CertificateService(config, keys, new FakeCertificateRepository(), new FakeCertificateAuthorityRepository(), baseUrlProvider.Object, log);
        _revocations = new CertificateRevokeService(new FakeRevocationRepository(), new FakeCrlRepository(), _certificates, keys, log, config);
    }

    [TestCase(EllipticCurve.P256, "SHA256WithECDSA")]
    [TestCase(EllipticCurve.P384, "SHA384WithECDSA")]
    [TestCase(EllipticCurve.P521, "SHA512WithECDSA")]
    public void ForIssuerKey_EcKey_MatchesTheCurve(EllipticCurve curve, string expected)
    {
        var key = GenerateEcKey(GetCurveOid(curve));

        Assert.That(SignatureAlgorithms.ForIssuerKey(key.Private), Is.EqualTo(expected));
    }

    [Test]
    public void ForIssuerKey_UnlistedCurve_FallsBackToSha256()
    {
        var key = GenerateEcKey(SecObjectIdentifiers.SecP224r1);

        Assert.That(SignatureAlgorithms.ForIssuerKey(key.Private), Is.EqualTo("SHA256WithECDSA"));
    }

    [TestCase(2048, "SHA256WithRSA")]
    [TestCase(2049, "SHA384WithRSA")]
    [TestCase(3072, "SHA384WithRSA")]
    [TestCase(3073, "SHA512WithRSA")]
    [TestCase(4096, "SHA512WithRSA")]
    [TestCase(1024, "SHA256WithRSA")]
    public void ForIssuerKey_RsaKey_ScalesWithTheModulusSize(int bits, string expected)
    {
        // The helper reads only the size, so a modulus of the right length stands in for a generated key.
        // BouncyCastle rejects even moduli and small prime factors, so step to the next odd number it accepts.
        var modulus = BigInteger.One.ShiftLeft(bits - 1).Add(BigInteger.One);
        RsaKeyParameters key = null;
        while (key == null)
        {
            try
            {
                key = new RsaKeyParameters(true, modulus, BigInteger.Three);
            }
            catch (ArgumentException)
            {
                modulus = modulus.Add(BigInteger.Two);
            }
        }

        Assert.That(SignatureAlgorithms.ForIssuerKey(key), Is.EqualTo(expected));
    }

    [Test]
    public void ForIssuerKey_UnsupportedKey_Throws()
    {
        Assert.Throws<NotaryException>(() => SignatureAlgorithms.ForIssuerKey(new Ed25519PrivateKeyParameters(new SecureRandom())));
        Assert.Throws<NotaryException>(() => SignatureAlgorithms.ForIssuerKey(null));
    }

    [TestCase(Algorithm.EllipticCurve, EllipticCurve.P256, null, "1.2.840.10045.4.3.2")]
    [TestCase(Algorithm.EllipticCurve, EllipticCurve.P384, null, "1.2.840.10045.4.3.3")]
    [TestCase(Algorithm.EllipticCurve, EllipticCurve.P521, null, "1.2.840.10045.4.3.4")]
    [TestCase(Algorithm.RSA, null, 2048, "1.2.840.113549.1.1.11")]
    [TestCase(Algorithm.RSA, null, 3072, "1.2.840.113549.1.1.12")]
    public async Task IssuedCertificatesAndCrl_AreSignedWithTheAlgorithmOfTheIssuerKey(
        Algorithm algorithm, EllipticCurve? curve, int? keySize, string expectedOid)
    {
        var root = await IssueAsync("root", algorithm, curve, keySize, isCa: true);
        var leaf = await IssueAsync("leaf", algorithm, curve, keySize, parent: root);
        await _revocations.RevokeCertificateAsync(leaf.Slug, RevocationReason.Superceded, "admin");

        var rootX509 = Parse(root);
        var leafX509 = Parse(leaf);
        Assert.That(rootX509.SigAlgOid, Is.EqualTo(expectedOid), "Self-signed root");
        Assert.That(leafX509.SigAlgOid, Is.EqualTo(expectedOid), "Leaf");
        rootX509.Verify(rootX509.GetPublicKey());
        leafX509.Verify(rootX509.GetPublicKey());

        var crl = await GetCrlAsync(root);
        Assert.That(crl.SigAlgOid, Is.EqualTo(expectedOid), "CRL");
        crl.Verify(rootX509.GetPublicKey());
    }

    // The key strength rule only lets an RSA leaf sit under an EC CA when it is at least as strong, so P-256 pairs with RSA-3072
    [Test]
    public async Task RsaLeafUnderEcCa_UsesTheEcIssuersAlgorithm()
    {
        var root = await IssueAsync("root", Algorithm.EllipticCurve, EllipticCurve.P256, null, isCa: true);
        var leaf = await IssueAsync("leaf", Algorithm.RSA, null, 3072, parent: root);

        var leafX509 = Parse(leaf);
        Assert.That(leafX509.SigAlgOid, Is.EqualTo(X9ObjectIdentifiers.ECDsaWithSha256.Id));
        leafX509.Verify(Parse(root).GetPublicKey());
    }

    [Test]
    public async Task EcLeafUnderRsaCa_UsesTheRsaIssuersAlgorithm()
    {
        var root = await IssueAsync("root", Algorithm.RSA, null, 2048, isCa: true);
        var leaf = await IssueAsync("leaf", Algorithm.EllipticCurve, EllipticCurve.P521, null, parent: root);

        var leafX509 = Parse(leaf);
        Assert.That(leafX509.SigAlgOid, Is.EqualTo(PkcsObjectIdentifiers.Sha256WithRsaEncryption.Id));
        leafX509.Verify(Parse(root).GetPublicKey());
    }

    private async Task<Certificate> IssueAsync(
        string name,
        Algorithm algorithm,
        EllipticCurve? curve,
        int? keySize,
        bool isCa = false,
        Certificate parent = null)
    {
        var usage = isCa
            ? (int)(CertificateKeyUsage.KeyCertSign | CertificateKeyUsage.CrlSign)
            : (int)CertificateKeyUsage.DigitalSignature;

        var result = await _certificates.IssueCertificateAsync(new CertificateRequest
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

        Assert.That(result.IsSuccess, Is.True, result.Error);
        return result.Value;
    }

    private async Task<X509Crl> GetCrlAsync(Certificate ca)
    {
        var result = await _revocations.GetCrlAsync(ca.Slug);
        Assert.That(result.IsSuccess, Is.True, result.Error);
        return new X509CrlParser().ReadCrl(result.Value.Data);
    }

    private static X509Certificate Parse(Certificate certificate) =>
        new X509CertificateParser().ReadCertificate(System.Text.Encoding.UTF8.GetBytes(certificate.Data));

    private static AsymmetricCipherKeyPair GenerateEcKey(Org.BouncyCastle.Asn1.DerObjectIdentifier curveOid)
    {
        var generator = new ECKeyPairGenerator();
        generator.Init(new ECKeyGenerationParameters(curveOid, new SecureRandom()));
        return generator.GenerateKeyPair();
    }

    private static Org.BouncyCastle.Asn1.DerObjectIdentifier GetCurveOid(EllipticCurve curve) => curve switch
    {
        EllipticCurve.P256 => SecObjectIdentifiers.SecP256r1,
        EllipticCurve.P384 => SecObjectIdentifiers.SecP384r1,
        _ => SecObjectIdentifiers.SecP521r1
    };
}
