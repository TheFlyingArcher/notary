using Notary.Configuration;
using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;
using Notary.Profiles;
using Notary.Service;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.X509;

namespace Notary.Test;

/// <summary>
/// Issues real CA certificates by purpose over in-memory storage and parses what was signed.
/// </summary>
public class CaPurposeIssuanceTest
{
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

    [TestCase(CaPurpose.TlsSsl, new[] { KeyPurposeOids.ServerAuthentication })]
    [TestCase(CaPurpose.UserLogin, new[] { KeyPurposeOids.ClientAuthentication, KeyPurposeOids.SmartCardLogin })]
    [TestCase(CaPurpose.Smime, new[] { KeyPurposeOids.EmailProtection })]
    [TestCase(CaPurpose.IoT, new[] { KeyPurposeOids.ClientAuthentication, KeyPurposeOids.ServerAuthentication })]
    [TestCase(CaPurpose.Dot1xRadius, new[] { KeyPurposeOids.ClientAuthentication, KeyPurposeOids.ServerAuthentication })]
    public async Task SelfSignedPurposeCa_CarriesItsUsages_AndNoPathLength(CaPurpose purpose, string[] expectedEkus)
    {
        var certificate = await CreateAndReadAsync(purpose);

        Assert.That(KeyUsageNames(certificate), Is.EquivalentTo(new[] { "KeyCertSign", "CrlSign" }));
        Assert.That(ExtendedKeyUsages(certificate), Is.EquivalentTo(expectedEkus));
        Assert.That(certificate.GetBasicConstraints(), Is.EqualTo(int.MaxValue), "a CA without a path length constraint");
        Assert.That(BasicConstraintsIsCritical(certificate), Is.True);
    }

    [Test]
    public async Task Root_CarriesNoExtendedKeyUsage_AndNoPathLength()
    {
        var certificate = await CreateAndReadAsync(CaPurpose.Root);

        Assert.That(KeyUsageNames(certificate), Is.EquivalentTo(new[] { "KeyCertSign", "CrlSign" }));
        Assert.That(ExtendedKeyUsages(certificate), Is.Empty);
        Assert.That(certificate.GetBasicConstraints(), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public async Task PurposeCaWithAParent_HasPathLengthZero()
    {
        var root = await CreateAsync(CaPurpose.Root);

        var certificate = await CreateAndReadAsync(CaPurpose.TlsSsl, parent: root);

        Assert.That(certificate.GetBasicConstraints(), Is.EqualTo(0));
        Assert.That(BasicConstraintsIsCritical(certificate), Is.True);
        Assert.That(ExtendedKeyUsages(certificate), Is.EquivalentTo(new[] { KeyPurposeOids.ServerAuthentication }));
    }

    [Test]
    public async Task CustomCa_CarriesChosenUsages_PlusCertSignAndCrlSign()
    {
        var certificate = await CreateAndReadAsync(
            CaPurpose.Custom,
            customKeyUsages: [CertificateKeyUsage.DigitalSignature],
            customExtendedKeyUsages: [KeyPurposeOids.ClientAuthentication]);

        Assert.That(KeyUsageNames(certificate), Is.EquivalentTo(new[] { "DigitalSignature", "KeyCertSign", "CrlSign" }));
        Assert.That(ExtendedKeyUsages(certificate), Is.EquivalentTo(new[] { KeyPurposeOids.ClientAuthentication }));
        Assert.That(certificate.GetBasicConstraints(), Is.EqualTo(int.MaxValue), "Custom is unconstrained");
    }

    [Test]
    public async Task CaWithoutAPurpose_KeepsTheLegacyBroadUsages_AndStaysUnpurposed()
    {
        var ca = NewCa(null);

        var result = await _service.CreateAsync(ca, null);
        var certificate = Parse(await _certificates.GetAsync(ca.CertificateSlug));

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(ca.Purpose, Is.Null, "a missing purpose is not rewritten");
        Assert.That(
            KeyUsageNames(certificate),
            Is.EquivalentTo(new[] { "KeyCertSign", "CrlSign", "DigitalSignature", "KeyEncipherment", "DataEncipherment" }));
        Assert.That(
            ExtendedKeyUsages(certificate),
            Is.EquivalentTo(new[] { "1.3.6.1.5.5.7.3.3", "1.3.6.1.5.5.7.3.1", "1.3.6.1.5.5.7.3.2", "1.3.6.1.4.1.311.20.2.2" }));
        Assert.That(certificate.GetBasicConstraints(), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public async Task RootWithAParent_IsRejectedAndNothingIsSaved()
    {
        var root = await CreateAsync(CaPurpose.Root);
        int caCount = (await _caRepo.GetAllAsync()).Count;
        int certificateCount = (await _certificateRepo.GetAllAsync()).Count;

        var result = await _service.CreateAsync(NewCa(CaPurpose.Root, parent: root), null);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That((await _caRepo.GetAllAsync()).Count, Is.EqualTo(caCount));
        Assert.That((await _certificateRepo.GetAllAsync()).Count, Is.EqualTo(certificateCount));
    }

    [Test]
    public async Task SmimeUnderTls_IsRejected()
    {
        var tls = await CreateAsync(CaPurpose.TlsSsl);

        var result = await _service.CreateAsync(NewCa(CaPurpose.Smime, parent: tls), null);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("E-mail Protection"));
    }

    [Test]
    public async Task CustomWithNoUsagesUnderARestrictedParent_IsRejected()
    {
        var tls = await CreateAsync(CaPurpose.TlsSsl);

        var result = await _service.CreateAsync(NewCa(CaPurpose.Custom, parent: tls), null);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("explicit usages"));
    }

    [Test]
    public async Task LegacyCaWithoutAPurposeUnderARestrictedParent_IsRejected()
    {
        var tls = await CreateAsync(CaPurpose.TlsSsl);

        var result = await _service.CreateAsync(NewCa(null, parent: tls), null);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task CaUnderAParentWithPathLengthZero_IsRejected()
    {
        var root = await CreateAsync(CaPurpose.Root);
        var tls = await CreateAsync(CaPurpose.TlsSsl, parent: root);

        var result = await _service.CreateAsync(NewCa(CaPurpose.TlsSsl, parent: tls), null);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("end-entity"));
    }

    [Test]
    public async Task SelfSignedPurposeCa_CanHaveASamePurposeChild()
    {
        var tls = await CreateAsync(CaPurpose.TlsSsl);

        var result = await _service.CreateAsync(NewCa(CaPurpose.TlsSsl, parent: tls), null);

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [Test]
    public async Task FullChain_RootToTlsIntermediateToServerLeaf_Issues()
    {
        var root = await CreateAsync(CaPurpose.Root);
        var intermediate = await CreateAsync(CaPurpose.TlsSsl, parent: root);

        var leaf = await _certificates.IssueCertificateAsync(new CertificateRequest
        {
            CertificateKeyUsageFlags = [(int)CertificateKeyUsage.DigitalSignature, (int)CertificateKeyUsage.KeyEncipherment],
            Curve = EllipticCurve.P256,
            ExtendedKeyUsages = [KeyPurposeOids.ServerAuthentication],
            IsCaCertificate = false,
            KeyAlgorithm = Algorithm.EllipticCurve,
            Name = "server",
            NotAfter = intermediate.NotAfter,
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            ParentCertificateSlug = intermediate.CertificateSlug,
            RequestedBySlug = "tester",
            Subject = new DistinguishedName { CommonName = "server.example" },
            SubjectAlternativeNames = []
        });

        Assert.That(leaf.IsSuccess, Is.True, leaf.Error);

        var leafCertificate = Parse(leaf.Value);
        var intermediateCertificate = Parse(await _certificates.GetAsync(intermediate.CertificateSlug));
        var rootCertificate = Parse(await _certificates.GetAsync(root.CertificateSlug));

        leafCertificate.Verify(intermediateCertificate.GetPublicKey());
        intermediateCertificate.Verify(rootCertificate.GetPublicKey());
        rootCertificate.Verify(rootCertificate.GetPublicKey());
        Assert.That(leafCertificate.GetBasicConstraints(), Is.EqualTo(-1), "a leaf is not a CA");
    }

    [Test]
    public async Task CommonNameWithPunctuation_IsIssuedAndReadBackExactly_AndStillChains()
    {
        const string commonName = "Acme, Inc. (Production) \"Root\" CA; R&D=1+2";
        var root = NewCa(CaPurpose.Root);
        root.DistinguishedName.CommonName = commonName;
        var rootResult = await _service.CreateAsync(root, null);
        Assert.That(rootResult.IsSuccess, Is.True, rootResult.Error);

        var child = await CreateAsync(CaPurpose.TlsSsl, parent: root);

        var rootCertificate = Parse(await _certificates.GetAsync(root.CertificateSlug));
        var childCertificate = Parse(await _certificates.GetAsync(child.CertificateSlug));

        Assert.That(rootCertificate.SubjectDN.GetValueList(X509Name.CN), Is.EqualTo(new[] { commonName }));
        Assert.That(rootCertificate.SubjectDN.GetValueList(X509Name.O), Is.Empty, "nothing was injected into the name");
        Assert.That(childCertificate.IssuerDN.Equivalent(rootCertificate.SubjectDN), Is.True, "the issuer matches the parent's subject");
        childCertificate.Verify(rootCertificate.GetPublicKey());
    }

    [Test]
    public async Task PlainNames_AreEncodedExactlyAsTheyWereBefore()
    {
        // Certificates issued before names were built from attributes were built by parsing text. For ordinary names
        // the encoding must be identical, so chains involving older CAs keep matching issuer to subject.
        var ca = NewCa(CaPurpose.Root);
        ca.DistinguishedName = new DistinguishedName
        {
            CommonName = "My Root CA",
            Country = "US",
            Locale = "San Francisco",
            Organization = "Acme-Corp_1",
            OrganizationalUnit = "IT.Security",
            StateProvince = "California"
        };

        var result = await _service.CreateAsync(ca, null);
        Assert.That(result.IsSuccess, Is.True, result.Error);

        var certificate = Parse(await _certificates.GetAsync(ca.CertificateSlug));
        var previousEncoding = new X509Name(DistinguishedName.BuildDistinguishedName(ca.DistinguishedName)).GetEncoded();

        Assert.That(certificate.SubjectDN.GetEncoded(), Is.EqualTo(previousEncoding));
        Assert.That(certificate.IssuerDN.GetEncoded(), Is.EqualTo(previousEncoding), "self-signed: issuer equals subject");
    }

    [Test]
    public async Task CaWithAnInvalidCommonName_ReturnsInvalidInsteadOfThrowing_AndSavesNothing()
    {
        var ca = NewCa(CaPurpose.Root);
        ca.DistinguishedName.CommonName = new string('a', 65);

        CertificateAuthority created = null;
        Result<CertificateAuthority> result = null;
        Assert.DoesNotThrowAsync(async () => result = await _service.CreateAsync(ca, null));

        Assert.That(created, Is.Null);
        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("64"));
        Assert.That(await _caRepo.GetAllAsync(), Is.Empty);
        Assert.That(await _certificateRepo.GetAllAsync(), Is.Empty);
    }

    [Test]
    public async Task CertificateWithAnInvalidSubject_ReturnsInvalidInsteadOfThrowing()
    {
        var request = Request(isCa: false, pathLength: -1);
        request.PathLengthConstraint = null;
        request.Subject = new DistinguishedName { CommonName = "tab\there" };

        Result<Certificate> result = null;
        Assert.DoesNotThrowAsync(async () => result = await _certificates.IssueCertificateAsync(request));

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("Common Name"));
        Assert.That(await _certificateRepo.GetAllAsync(), Is.Empty);
    }

    [TestCase(new[] { CertificateKeyUsage.DigitalSignature, CertificateKeyUsage.KeyEncipherment }, new[] { "DigitalSignature", "KeyEncipherment" })]
    [TestCase(new[] { CertificateKeyUsage.DigitalSignature }, new[] { "DigitalSignature" })]
    public async Task LeafKeyUsages_AreEncodedExactlyAsRequested(CertificateKeyUsage[] requested, string[] expectedNames)
    {
        var root = await CreateAsync(CaPurpose.Root);
        var tls = await CreateAsync(CaPurpose.TlsSsl, parent: root);

        var leaf = await _certificates.IssueCertificateAsync(new CertificateRequest
        {
            CertificateKeyUsageFlags = [.. requested.Select(u => (int)u)],
            Curve = EllipticCurve.P256,
            ExtendedKeyUsages = [KeyPurposeOids.ServerAuthentication],
            IsCaCertificate = false,
            KeyAlgorithm = Algorithm.EllipticCurve,
            Name = "leaf",
            NotAfter = tls.NotAfter,
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            ParentCertificateSlug = tls.CertificateSlug,
            RequestedBySlug = "tester",
            Subject = new DistinguishedName { CommonName = "leaf.example" },
            SubjectAlternativeNames = []
        });

        Assert.That(leaf.IsSuccess, Is.True, leaf.Error);
        Assert.That(KeyUsageNames(Parse(leaf.Value)), Is.EquivalentTo(expectedNames));
    }

    private static readonly CertificateKeyUsage[] SignAndEncipher =
        [CertificateKeyUsage.DigitalSignature, CertificateKeyUsage.KeyEncipherment];

    private static CertificateRequest LeafRequest(CertificateAuthority issuer, CertificateKeyUsage[] keyUsages, string[] extendedKeyUsages)
    {
        return new CertificateRequest
        {
            CertificateKeyUsageFlags = [.. keyUsages.Select(u => (int)u)],
            Curve = EllipticCurve.P256,
            ExtendedKeyUsages = extendedKeyUsages,
            IsCaCertificate = false,
            KeyAlgorithm = Algorithm.EllipticCurve,
            Name = "leaf",
            NotAfter = issuer?.NotAfter ?? DateTime.UtcNow.AddYears(1),
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            ParentCertificateSlug = issuer?.CertificateSlug,
            RequestedBySlug = "tester",
            Subject = new DistinguishedName { CommonName = "leaf.example" },
            SubjectAlternativeNames = []
        };
    }

    [TestCase(CaPurpose.TlsSsl)]
    [TestCase(CaPurpose.UserLogin)]
    [TestCase(CaPurpose.Smime)]
    [TestCase(CaPurpose.IoT)]
    [TestCase(CaPurpose.Dot1xRadius)]
    public async Task PurposeCa_IssuesALeafWithItsOwnProfile(CaPurpose purpose)
    {
        var ca = await CreateAsync(purpose);
        var profile = CaProfileCatalog.Get(purpose);

        var result = await _certificates.IssueCertificateAsync(
            LeafRequest(ca, [.. profile.LeafKeyUsages], [.. profile.LeafExtendedKeyUsages]));

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(ExtendedKeyUsages(Parse(result.Value)), Is.EquivalentTo(profile.LeafExtendedKeyUsages));
    }

    [Test]
    public async Task TlsCa_RejectsALeafWithClientAuthenticationAdded_AndSavesNothing()
    {
        // The UAT case: a leaf asking for Server and Client Authentication from a server-only CA
        var tls = await CreateAsync(CaPurpose.TlsSsl);
        int saved = (await _certificateRepo.GetAllAsync()).Count;

        var result = await _certificates.IssueCertificateAsync(
            LeafRequest(tls, SignAndEncipher, [KeyPurposeOids.ServerAuthentication, KeyPurposeOids.ClientAuthentication]));

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("Client Authentication").And.Contain("TLS/SSL"));
        Assert.That((await _certificateRepo.GetAllAsync()).Count, Is.EqualTo(saved), "nothing was issued");
    }

    [Test]
    public async Task TlsCa_RejectsALeafWithAnUnpermittedKeyUsage()
    {
        var tls = await CreateAsync(CaPurpose.TlsSsl);

        var result = await _certificates.IssueCertificateAsync(
            LeafRequest(tls, [CertificateKeyUsage.EncipherOnly], [KeyPurposeOids.ServerAuthentication]));

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("Encipher Only"));
    }

    [Test]
    public async Task TlsCa_RejectsALeafWithNoExtendedKeyUsage()
    {
        var tls = await CreateAsync(CaPurpose.TlsSsl);

        var result = await _certificates.IssueCertificateAsync(LeafRequest(tls, SignAndEncipher, []));

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task Root_NeverIssuesALeaf()
    {
        var root = await CreateAsync(CaPurpose.Root);

        var result = await _certificates.IssueCertificateAsync(
            LeafRequest(root, SignAndEncipher, [KeyPurposeOids.ServerAuthentication]));

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("intermediate"));
    }

    [Test]
    public async Task SelfSignedCertificate_KeepsAdvancedUsages()
    {
        var result = await _certificates.IssueCertificateAsync(LeafRequest(
            null,
            [CertificateKeyUsage.EncipherOnly, CertificateKeyUsage.NonRepudiation],
            [KeyPurposeOids.ServerAuthentication, KeyPurposeOids.ClientAuthentication, KeyPurposeOids.EmailProtection]));

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [Test]
    public async Task LegacyCaWithoutAPurpose_IsUnrestricted()
    {
        var legacy = NewCa(null);
        var created = await _service.CreateAsync(legacy, null);
        Assert.That(created.IsSuccess, Is.True, created.Error);

        var result = await _certificates.IssueCertificateAsync(LeafRequest(
            legacy,
            [CertificateKeyUsage.NonRepudiation],
            [KeyPurposeOids.EmailProtection]));

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [Test]
    public async Task CustomCaWithChosenExtendedKeyUsages_LimitsTheLeafsExtendedKeyUsages()
    {
        var custom = await CreateAsync(
            CaPurpose.Custom,
            customKeyUsages: [CertificateKeyUsage.DigitalSignature],
            customExtendedKeyUsages: [KeyPurposeOids.ServerAuthentication]);

        var allowed = await _certificates.IssueCertificateAsync(
            LeafRequest(custom, [CertificateKeyUsage.EncipherOnly], [KeyPurposeOids.ServerAuthentication]));
        var rejected = await _certificates.IssueCertificateAsync(
            LeafRequest(custom, SignAndEncipher, [KeyPurposeOids.ServerAuthentication, KeyPurposeOids.ClientAuthentication]));

        Assert.That(allowed.IsSuccess, Is.True, allowed.Error);
        Assert.That(rejected.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task IssuingCertificateWithNoCaRecord_IsUnrestricted()
    {
        // Certificates issued directly, with no CA record behind them, have no purpose to follow
        var issuer = await _certificates.IssueCertificateAsync(new CertificateRequest
        {
            CertificateKeyUsageFlags = [(int)CertificateKeyUsage.KeyCertSign, (int)CertificateKeyUsage.CrlSign],
            Curve = EllipticCurve.P256,
            ExtendedKeyUsages = [],
            IsCaCertificate = true,
            KeyAlgorithm = Algorithm.EllipticCurve,
            Name = "bare-ca",
            NotAfter = DateTime.UtcNow.AddYears(5),
            NotBefore = DateTime.UtcNow.AddMinutes(-5),
            RequestedBySlug = "tester",
            Subject = new DistinguishedName { CommonName = "bare-ca" },
            SubjectAlternativeNames = []
        });
        Assert.That(issuer.IsSuccess, Is.True, issuer.Error);

        var request = LeafRequest(null, SignAndEncipher, [KeyPurposeOids.EmailProtection]);
        request.ParentCertificateSlug = issuer.Value.Slug;
        request.NotAfter = issuer.Value.NotAfter;

        var result = await _certificates.IssueCertificateAsync(request);

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [Test]
    public async Task CustomCaCreatedLikeThePageDoes_WithNothingExtra_GetsOnlyTheTwoFixedKeyUsages()
    {
        // The page always sends the two fixed key usages, so a Custom CA with nothing else chosen is explicit and is
        // not mistaken for a CA created without a purpose
        var certificate = await CreateAndReadAsync(
            CaPurpose.Custom,
            customKeyUsages: [CertificateKeyUsage.KeyCertSign, CertificateKeyUsage.CrlSign],
            customExtendedKeyUsages: []);

        Assert.That(KeyUsageNames(certificate), Is.EquivalentTo(new[] { "KeyCertSign", "CrlSign" }));
        Assert.That(ExtendedKeyUsages(certificate), Is.Empty, "not the previous broad set of extended key usages");
        Assert.That(certificate.GetBasicConstraints(), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public async Task CustomCaCreatedLikeThePageDoes_WithExtras_GetsExactlyWhatWasChosen()
    {
        var certificate = await CreateAndReadAsync(
            CaPurpose.Custom,
            customKeyUsages:
            [
                CertificateKeyUsage.KeyCertSign,
                CertificateKeyUsage.CrlSign,
                CertificateKeyUsage.DigitalSignature
            ],
            customExtendedKeyUsages: [KeyPurposeOids.ClientAuthentication, KeyPurposeOids.CodeSigning]);

        Assert.That(KeyUsageNames(certificate), Is.EquivalentTo(new[] { "KeyCertSign", "CrlSign", "DigitalSignature" }));
        Assert.That(
            ExtendedKeyUsages(certificate),
            Is.EquivalentTo(new[] { KeyPurposeOids.ClientAuthentication, KeyPurposeOids.CodeSigning }));
    }

    [Test]
    public async Task CustomCaUnderATlsParent_MustListServerAuthenticationOnly()
    {
        var tls = await CreateAsync(CaPurpose.TlsSsl);
        CertificateKeyUsage[] fixedUsages = [CertificateKeyUsage.KeyCertSign, CertificateKeyUsage.CrlSign];

        var allowed = await _service.CreateAsync(
            NewCa(CaPurpose.Custom, tls, [.. fixedUsages], [KeyPurposeOids.ServerAuthentication]),
            null);
        var wrongUsage = await _service.CreateAsync(
            NewCa(CaPurpose.Custom, tls, [.. fixedUsages], [KeyPurposeOids.ServerAuthentication, KeyPurposeOids.ClientAuthentication]),
            null);
        var noUsages = await _service.CreateAsync(NewCa(CaPurpose.Custom, tls, [.. fixedUsages], []), null);

        Assert.That(allowed.IsSuccess, Is.True, allowed.Error);
        Assert.That(wrongUsage.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(wrongUsage.Error, Does.Contain("Client Authentication"));
        Assert.That(noUsages.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(noUsages.Error, Does.Contain("explicit usages"));
    }

    [Test]
    public async Task NegativePathLength_IsRejected()
    {
        var result = await _certificates.IssueCertificateAsync(Request(isCa: true, pathLength: -1));

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task PathLengthOnANonCaCertificate_IsRejected()
    {
        var result = await _certificates.IssueCertificateAsync(Request(isCa: false, pathLength: 0));

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    private async Task<CertificateAuthority> CreateAsync(
        CaPurpose purpose,
        CertificateAuthority parent = null,
        List<CertificateKeyUsage> customKeyUsages = null,
        List<string> customExtendedKeyUsages = null)
    {
        var ca = NewCa(purpose, parent, customKeyUsages, customExtendedKeyUsages);
        var result = await _service.CreateAsync(ca, null);
        Assert.That(result.IsSuccess, Is.True, result.Error);
        return ca;
    }

    private async Task<X509Certificate> CreateAndReadAsync(
        CaPurpose purpose,
        CertificateAuthority parent = null,
        List<CertificateKeyUsage> customKeyUsages = null,
        List<string> customExtendedKeyUsages = null)
    {
        var ca = await CreateAsync(purpose, parent, customKeyUsages, customExtendedKeyUsages);
        return Parse(await _certificates.GetAsync(ca.CertificateSlug));
    }

    private static CertificateAuthority NewCa(
        CaPurpose? purpose,
        CertificateAuthority parent = null,
        List<CertificateKeyUsage> customKeyUsages = null,
        List<string> customExtendedKeyUsages = null)
    {
        string name = $"ca-{Guid.NewGuid():N}";

        return new CertificateAuthority
        {
            Active = true,
            Created = DateTime.UtcNow,
            CreatedBySlug = "tester",
            CustomExtendedKeyUsages = customExtendedKeyUsages,
            CustomKeyUsages = customKeyUsages,
            DistinguishedName = new DistinguishedName { CommonName = name, Country = "US" },
            IsIssuer = true,
            KeyAlgorithm = Algorithm.EllipticCurve,
            KeyCurve = EllipticCurve.P256,
            Name = name,
            NotAfter = parent?.NotAfter ?? DateTime.UtcNow.AddYears(10),
            NotBefore = DateTime.UtcNow.AddMinutes(-5),
            ParentCaSlug = parent?.Slug,
            Purpose = purpose
        };
    }

    private static CertificateRequest Request(bool isCa, int pathLength)
    {
        return new CertificateRequest
        {
            CertificateKeyUsageFlags = [(int)CertificateKeyUsage.DigitalSignature],
            Curve = EllipticCurve.P256,
            ExtendedKeyUsages = [],
            IsCaCertificate = isCa,
            KeyAlgorithm = Algorithm.EllipticCurve,
            Name = "request",
            NotAfter = DateTime.UtcNow.AddYears(1),
            NotBefore = DateTime.UtcNow.AddMinutes(-5),
            PathLengthConstraint = pathLength,
            RequestedBySlug = "tester",
            Subject = new DistinguishedName { CommonName = "request" },
            SubjectAlternativeNames = []
        };
    }

    private static X509Certificate Parse(Certificate certificate) =>
        new X509CertificateParser().ReadCertificate(System.Text.Encoding.UTF8.GetBytes(certificate.Data));

    private static string[] KeyUsageNames(X509Certificate certificate)
    {
        // BouncyCastle exposes key usage as the nine RFC 5280 bits, in order
        string[] names =
        [
            "DigitalSignature", "NonRepudiation", "KeyEncipherment", "DataEncipherment", "KeyAgreement",
            "KeyCertSign", "CrlSign", "EncipherOnly", "DecipherOnly"
        ];

        bool[] bits = certificate.GetKeyUsage();
        return [.. names.Where((_, index) => index < bits.Length && bits[index])];
    }

    private static string[] ExtendedKeyUsages(X509Certificate certificate)
    {
        var usages = certificate.GetExtendedKeyUsage();
        return usages == null ? [] : [.. usages.Select(u => u.Id)];
    }

    private static bool BasicConstraintsIsCritical(X509Certificate certificate) =>
        certificate.GetCriticalExtensionOids().Contains(X509Extensions.BasicConstraints.Id);
}
