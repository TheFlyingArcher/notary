using Notary.Configuration;
using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;
using Notary.Service;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;

namespace Notary.Test;

/// <summary>
/// Exercises revocation and CRL publication end to end over real certificates, with in-memory storage.
/// </summary>
public class CertificateRevokeServiceTest
{
    private const string CrlEndpoint = "http://pki.test/api/crl";

    private FakeKeyService _keys;
    private FakeRepository<Certificate> _certificateRepo;
    private FakeRevocationRepository _revocationRepo;
    private FakeCrlRepository _crlRepo;
    private CertificateService _certificates;
    private CertificateRevokeService _service;

    [SetUp]
    public void SetUp()
    {
        var config = new NotaryConfiguration { CrlEndpoint = CrlEndpoint };
        var log = new Mock<ILog>().Object;

        _keys = new FakeKeyService();
        _certificateRepo = new FakeCertificateRepository();
        _revocationRepo = new FakeRevocationRepository();
        _crlRepo = new FakeCrlRepository();

        _certificates = new CertificateService(config, _keys, (ICertificateRepository)_certificateRepo, log);
        _service = new CertificateRevokeService(_revocationRepo, _crlRepo, _certificates, _keys, log, config);
    }

    [Test]
    public async Task IssuedCertificateCrlDistributionPointNamesTheIssuer()
    {
        var root = await IssueAsync("root", isCa: true);
        var intermediate = await IssueAsync("intermediate", isCa: true, parent: root);
        var leaf = await IssueAsync("leaf", parent: intermediate);

        Assert.That(GetCrlUrl(root), Is.Null, "A self-signed root has no CRL distribution point");
        Assert.That(GetCrlUrl(intermediate), Is.EqualTo($"{CrlEndpoint}/{root.Slug}"));
        Assert.That(GetCrlUrl(leaf), Is.EqualTo($"{CrlEndpoint}/{intermediate.Slug}"));
    }

    [Test]
    public async Task AuthorityKeyIdentifierMatchesTheIssuersSubjectKeyIdentifier()
    {
        var root = await IssueAsync("root", isCa: true);
        var intermediate = await IssueAsync("intermediate", isCa: true, parent: root);
        var leaf = await IssueAsync("leaf", parent: intermediate);

        var aki = AuthorityKeyIdentifier.GetInstance(
            X509ExtensionUtilities.FromExtensionValue(Parse(leaf).GetExtensionValue(X509Extensions.AuthorityKeyIdentifier)));
        var ski = SubjectKeyIdentifier.GetInstance(
            X509ExtensionUtilities.FromExtensionValue(Parse(intermediate).GetExtensionValue(X509Extensions.SubjectKeyIdentifier)));

        Assert.That(aki.GetKeyIdentifier(), Is.EqualTo(ski.GetKeyIdentifier()));
        Assert.That(aki.AuthorityCertIssuer, Is.Null, "Naming the issuer certificate wrongly breaks chain building");
    }

    [Test]
    public async Task CrlListsOnlyCertificatesIssuedByTheCa()
    {
        var root = await IssueAsync("root", isCa: true);
        var ca1 = await IssueAsync("ca1", isCa: true, parent: root);
        var ca2 = await IssueAsync("ca2", isCa: true, parent: root);
        var leaf1 = await IssueAsync("leaf1", parent: ca1);
        var leaf2 = await IssueAsync("leaf2", parent: ca2);

        var result = await _service.RevokeCertificateAsync(leaf1.Slug, RevocationReason.KeyCompromized, "admin");
        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(result.Value, Is.EqualTo(1));

        var ca1Crl = await GetCrlAsync(ca1);
        var ca2Crl = await GetCrlAsync(ca2);

        Assert.That(ca1Crl.IsRevoked(Serial(leaf1)), Is.True);
        Assert.That(ca1Crl.IsRevoked(Serial(leaf2)), Is.False);
        Assert.That(ca2Crl.GetRevokedCertificates(), Is.Null.Or.Empty);

        var entry = ca1Crl.GetRevokedCertificate(Serial(leaf1));
        Assert.That(ReadReason(entry), Is.EqualTo(CrlReason.KeyCompromise));
        Assert.That(entry.GetExtensionValue(X509Extensions.InvalidityDate), Is.Not.Null, "Key compromise records an invalidity date");
    }

    [Test]
    public async Task CrlIsSignedByTheIssuerAndWellFormed()
    {
        var root = await IssueAsync("root", isCa: true);
        var leaf = await IssueAsync("leaf", parent: root);
        await _service.RevokeCertificateAsync(leaf.Slug, RevocationReason.Superceded, "admin");

        var result = await _service.GetCrlAsync(root.Slug);
        Assert.That(result.IsSuccess, Is.True, result.Error);

        var crl = new X509CrlParser().ReadCrl(result.Value.Data);
        var rootX509 = Parse(root);
        crl.Verify(rootX509.GetPublicKey());

        Assert.That(crl.IssuerDN.ToString(), Is.EqualTo(rootX509.SubjectDN.ToString()));
        Assert.That(crl.NextUpdate.Value - crl.ThisUpdate, Is.EqualTo(TimeSpan.FromDays(7)));
        Assert.That(crl.GetExtensionValue(X509Extensions.AuthorityKeyIdentifier), Is.Not.Null);
        Assert.That(ReadCrlNumber(crl), Is.EqualTo(result.Value.Number));
        Assert.That(ReadReason(crl.GetRevokedCertificate(Serial(leaf))), Is.EqualTo(CrlReason.Superseded));
    }

    [Test]
    public async Task UnspecifiedReasonOmitsTheReasonCode()
    {
        var root = await IssueAsync("root", isCa: true);
        var leaf = await IssueAsync("leaf", parent: root);
        await _service.RevokeCertificateAsync(leaf.Slug, RevocationReason.Unspecified, "admin");

        var entry = (await GetCrlAsync(root)).GetRevokedCertificate(Serial(leaf));

        Assert.That(entry.GetExtensionValue(X509Extensions.ReasonCode), Is.Null);
    }

    [Test]
    public async Task CrlNumberIncreasesOnEachIssuanceAndCachedCrlIsServed()
    {
        var root = await IssueAsync("root", isCa: true);
        var leaf1 = await IssueAsync("leaf1", parent: root);
        var leaf2 = await IssueAsync("leaf2", parent: root);

        await _service.RevokeCertificateAsync(leaf1.Slug, RevocationReason.Unspecified, "admin");
        var first = (await _service.GetCrlAsync(root.Slug)).Value;
        var cached = (await _service.GetCrlAsync(root.Slug)).Value;
        Assert.That(cached.Number, Is.EqualTo(first.Number), "A fresh CRL is reused, not re-signed");

        await _service.RevokeCertificateAsync(leaf2.Slug, RevocationReason.Unspecified, "admin");
        var second = (await _service.GetCrlAsync(root.Slug)).Value;
        Assert.That(second.Number, Is.EqualTo(first.Number + 1));
    }

    [Test]
    public async Task StaleCrlIsReissuedOnRequest()
    {
        var root = await IssueAsync("root", isCa: true);
        var first = (await _service.GetCrlAsync(root.Slug)).Value;

        // Age the stored CRL beyond half of its validity
        var record = await _crlRepo.GetAsync(CrlRecord.SlugFor(root.Slug));
        record.ThisUpdate = record.ThisUpdate.AddDays(-5);
        record.NextUpdate = record.NextUpdate.AddDays(-5);
        await _crlRepo.SaveAsync(record);

        var second = (await _service.GetCrlAsync(root.Slug)).Value;

        Assert.That(second.Number, Is.EqualTo(first.Number + 1));
        Assert.That(second.NextUpdate, Is.GreaterThan(DateTime.UtcNow.AddDays(6)));
    }

    [Test]
    public async Task RevokingTwiceIsRejected()
    {
        var root = await IssueAsync("root", isCa: true);
        var leaf = await IssueAsync("leaf", parent: root);

        await _service.RevokeCertificateAsync(leaf.Slug, RevocationReason.Unspecified, "admin");
        var again = await _service.RevokeCertificateAsync(leaf.Slug, RevocationReason.KeyCompromized, "admin");

        Assert.That(again.Status, Is.EqualTo(ResultStatus.Conflict));
        Assert.That((await _service.GetRevocatedCertificates()).Count, Is.EqualTo(1));
    }

    [Test]
    public async Task RevocationRecordsUtcTimeAndIssuer()
    {
        var root = await IssueAsync("root", isCa: true);
        var leaf = await IssueAsync("leaf", parent: root);
        var before = DateTime.UtcNow.AddSeconds(-1);

        await _service.RevokeCertificateAsync(leaf.Slug, RevocationReason.CessationOfOperation, "admin");

        var revocation = await _service.GetRevocationAsync(leaf.Slug);
        Assert.That(revocation.IssuerSlug, Is.EqualTo(root.Slug));
        Assert.That(revocation.RevocationDate, Is.InRange(before, DateTime.UtcNow.AddSeconds(1)));
        Assert.That(revocation.RevocationDate.Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That((await _certificates.GetAsync(leaf.Slug)).RevocationDate, Is.Not.Null);
    }

    [Test]
    public async Task InvalidReasonsAreRejected()
    {
        var root = await IssueAsync("root", isCa: true);
        var leaf = await IssueAsync("leaf", parent: root);

        Assert.That((await _service.RevokeCertificateAsync(leaf.Slug, RevocationReason.RemoveFromCrl, "admin")).Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That((await _service.RevokeCertificateAsync(leaf.Slug, (RevocationReason)7, "admin")).Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That((await _service.RevokeCertificateAsync("missing", RevocationReason.Unspecified, "admin")).Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task RevokingACaRevokesEverythingItIssuedAsCaCompromised()
    {
        var root = await IssueAsync("root", isCa: true);
        var intermediate = await IssueAsync("intermediate", isCa: true, parent: root);
        var subCa = await IssueAsync("subca", isCa: true, parent: intermediate);
        var leafA = await IssueAsync("leaf-a", parent: intermediate);
        var leafB = await IssueAsync("leaf-b", parent: subCa);
        var unrelated = await IssueAsync("unrelated", parent: root);
        // Already revoked before the CA is, and must keep its original reason
        await _service.RevokeCertificateAsync(leafA.Slug, RevocationReason.Superceded, "admin");

        var result = await _service.RevokeCertificateAsync(intermediate.Slug, RevocationReason.KeyCompromized, "admin");

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(result.Value, Is.EqualTo(3), "intermediate + subCa + leafB; leafA was already revoked");

        Assert.That((await _service.GetRevocationAsync(intermediate.Slug)).Reason, Is.EqualTo(RevocationReason.KeyCompromized));
        Assert.That((await _service.GetRevocationAsync(subCa.Slug)).Reason, Is.EqualTo(RevocationReason.CaCompromized));
        Assert.That((await _service.GetRevocationAsync(leafB.Slug)).Reason, Is.EqualTo(RevocationReason.CaCompromized));
        Assert.That((await _service.GetRevocationAsync(leafA.Slug)).Reason, Is.EqualTo(RevocationReason.Superceded));
        Assert.That(await _service.GetRevocationAsync(unrelated.Slug), Is.Null);

        // Each issuer's CRL carries the certificates it issued
        var rootCrl = await GetCrlAsync(root);
        Assert.That(rootCrl.IsRevoked(Serial(intermediate)), Is.True);
        Assert.That(rootCrl.IsRevoked(Serial(unrelated)), Is.False);

        var intermediateCrl = await GetCrlAsync(intermediate);
        Assert.That(intermediateCrl.IsRevoked(Serial(subCa)), Is.True);
        Assert.That(ReadReason(intermediateCrl.GetRevokedCertificate(Serial(subCa))), Is.EqualTo(CrlReason.CACompromise));
        Assert.That(ReadReason(intermediateCrl.GetRevokedCertificate(Serial(leafA))), Is.EqualTo(CrlReason.Superseded));

        var subCaCrl = await GetCrlAsync(subCa);
        Assert.That(subCaCrl.IsRevoked(Serial(leafB)), Is.True);
    }

    [Test]
    public async Task CaOnHoldDoesNotCascadeAndCanBeReinstated()
    {
        var root = await IssueAsync("root", isCa: true);
        var intermediate = await IssueAsync("intermediate", isCa: true, parent: root);
        var leaf = await IssueAsync("leaf", parent: intermediate);

        var result = await _service.RevokeCertificateAsync(intermediate.Slug, RevocationReason.CertificateHold, "admin");
        Assert.That(result.Value, Is.EqualTo(1));
        Assert.That(await _service.GetRevocationAsync(leaf.Slug), Is.Null);
        Assert.That((await GetCrlAsync(root)).IsRevoked(Serial(intermediate)), Is.True);

        var reinstated = await _service.ReinstateCertificateAsync(intermediate.Slug, "admin");
        Assert.That(reinstated.IsSuccess, Is.True, reinstated.Error);
        Assert.That((await GetCrlAsync(root)).IsRevoked(Serial(intermediate)), Is.False);
        Assert.That((await _certificates.GetAsync(intermediate.Slug)).RevocationDate, Is.Null);

        // And it can be revoked again afterwards
        Assert.That((await _service.RevokeCertificateAsync(intermediate.Slug, RevocationReason.Unspecified, "admin")).IsSuccess, Is.True);
    }

    [Test]
    public async Task OnlyHoldsCanBeReinstated()
    {
        var root = await IssueAsync("root", isCa: true);
        var leaf = await IssueAsync("leaf", parent: root);

        Assert.That((await _service.ReinstateCertificateAsync(leaf.Slug, "admin")).Status, Is.EqualTo(ResultStatus.Conflict), "Not revoked");

        await _service.RevokeCertificateAsync(leaf.Slug, RevocationReason.KeyCompromized, "admin");
        Assert.That((await _service.ReinstateCertificateAsync(leaf.Slug, "admin")).Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That((await GetCrlAsync(root)).IsRevoked(Serial(leaf)), Is.True);
    }

    [Test]
    public async Task CrlIsNotIssuedForUnknownOrNonCaCertificates()
    {
        var root = await IssueAsync("root", isCa: true);
        var leaf = await IssueAsync("leaf", parent: root);

        Assert.That((await _service.GetCrlAsync("missing")).Status, Is.EqualTo(ResultStatus.NotFound));
        Assert.That((await _service.GetCrlAsync(leaf.Slug)).Status, Is.EqualTo(ResultStatus.NotFound));
        Assert.That((await _service.GetCrlAsync("")).Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task CrlIsNotIssuedByACaThatCannotSignCrls()
    {
        var root = await IssueAsync("root", isCa: true);
        var noCrlSign = await IssueAsync("nocrlsign", isCa: true, parent: root, keyUsage: (int)CertificateKeyUsage.KeyCertSign);

        Assert.That((await _service.GetCrlAsync(noCrlSign.Slug)).Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public async Task LegacyRevocationsAreAssignedToTheirIssuerOnInitialize()
    {
        var root = await IssueAsync("root", isCa: true);
        var leaf = await IssueAsync("leaf", parent: root);
        await _revocationRepo.SaveAsync(new RevocatedCertificate
        {
            Active = true,
            CertificateSlug = leaf.Slug,
            Created = DateTime.UtcNow.AddDays(-2),
            Reason = RevocationReason.KeyCompromized,
            SerialNumber = leaf.SerialNumber,
            Thumbprint = leaf.Thumbprint
        });

        await _service.InitializeAsync();

        var crl = await GetCrlAsync(root);
        Assert.That(crl.IsRevoked(Serial(leaf)), Is.True);
        Assert.That(crl.GetRevokedCertificate(Serial(leaf)).RevocationDate, Is.LessThan(DateTime.UtcNow.AddDays(-1)));
    }

    private async Task<Certificate> IssueAsync(
        string name,
        bool isCa = false,
        Certificate parent = null,
        int? keyUsage = null)
    {
        var usage = keyUsage ?? (isCa
            ? (int)(CertificateKeyUsage.KeyCertSign | CertificateKeyUsage.CrlSign)
            : (int)CertificateKeyUsage.DigitalSignature);

        return await _certificates.IssueCertificateAsync(new CertificateRequest
        {
            CertificateKeyUsageFlags = [usage],
            Curve = EllipticCurve.P256,
            ExtendedKeyUsages = [],
            IsCaCertificate = isCa,
            KeyAlgorithm = Algorithm.EllipticCurve,
            Name = name,
            NotAfter = DateTime.UtcNow.AddYears(1),
            NotBefore = DateTime.UtcNow.AddMinutes(-5),
            ParentCertificateSlug = parent?.Slug,
            RequestedBySlug = "tester",
            Subject = new DistinguishedName { CommonName = name },
            SubjectAlternativeNames = []
        });
    }

    private async Task<X509Crl> GetCrlAsync(Certificate ca)
    {
        var result = await _service.GetCrlAsync(ca.Slug);
        Assert.That(result.IsSuccess, Is.True, result.Error);
        var crl = new X509CrlParser().ReadCrl(result.Value.Data);
        crl.Verify(Parse(ca).GetPublicKey());
        return crl;
    }

    private static X509Certificate Parse(Certificate certificate) =>
        new X509CertificateParser().ReadCertificate(System.Text.Encoding.UTF8.GetBytes(certificate.Data));

    private static BigInteger Serial(Certificate certificate) => new(certificate.SerialNumber, 16);

    private static int ReadReason(X509CrlEntry entry) =>
        DerEnumerated.GetInstance(X509ExtensionUtilities.FromExtensionValue(entry.GetExtensionValue(X509Extensions.ReasonCode))).IntValueExact;

    private static long ReadCrlNumber(X509Crl crl) =>
        DerInteger.GetInstance(X509ExtensionUtilities.FromExtensionValue(crl.GetExtensionValue(X509Extensions.CrlNumber))).LongValueExact;

    private static string GetCrlUrl(Certificate certificate)
    {
        var extension = Parse(certificate).GetExtensionValue(X509Extensions.CrlDistributionPoints);
        if (extension == null)
            return null;

        var points = CrlDistPoint.GetInstance(X509ExtensionUtilities.FromExtensionValue(extension)).GetDistributionPoints();
        var names = GeneralNames.GetInstance(points[0].DistributionPointName.Name).GetNames();
        return DerIA5String.GetInstance(names[0].Name).GetString();
    }

    #region In-memory fakes

    private class FakeRepository<T> : IRepository<T> where T : Entity
    {
        protected readonly Dictionary<string, T> Items = [];

        public Task DeleteAsync(string slug, string updatedBySlug)
        {
            if (Items.TryGetValue(slug, out var item))
                item.Active = false;
            return Task.CompletedTask;
        }

        public Task DeleteByIdAsync(string id, string updatedBySlug) => DeleteAsync(id, updatedBySlug);

        public Task<T> GetAsync(string slug) => Task.FromResult(Items.TryGetValue(slug, out var item) ? Clone(item) : null);

        public Task<List<T>> GetAllAsync() => Task.FromResult(Items.Values.Select(Clone).ToList());

        public Task<T> GetByIdAsync(string id) => GetAsync(id);

        public Task InitializeAsync() => Task.CompletedTask;

        public Task SaveAsync(T entity)
        {
            if (string.IsNullOrEmpty(entity.Slug))
                entity.Slug = entity.Slugify();

            Items[entity.Slug] = Clone(entity);
            return Task.CompletedTask;
        }

        // Mimic a database: callers get copies, so changes only persist through SaveAsync
        protected static T Clone(T item) => (T)typeof(object)
            .GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(item, null)!;
    }

    private class FakeCertificateRepository : FakeRepository<Certificate>, ICertificateRepository
    {
        public Task<List<Certificate>> GetCertificatesByCaAsync(string caSlug) =>
            Task.FromResult(Items.Values.Where(c => c.IssuingSlug == caSlug).Select(Clone).ToList());
    }

    private class FakeCrlRepository : FakeRepository<CrlRecord>, ICrlRecordRepository
    {
    }

    private class FakeRevocationRepository : FakeRepository<RevocatedCertificate>, IRevocatedCertificateRepository
    {
        public Task<List<RevocatedCertificate>> GetActiveByIssuerAsync(string issuerSlug) =>
            Task.FromResult(Items.Values.Where(r => r.Active && r.IssuerSlug == issuerSlug).Select(Clone).ToList());

        public Task<RevocatedCertificate> GetActiveByCertificateAsync(string certificateSlug) =>
            Task.FromResult(Items.Values.Where(r => r.Active && r.CertificateSlug == certificateSlug).Select(Clone).FirstOrDefault());

        public Task<List<RevocatedCertificate>> GetWithoutIssuerAsync() =>
            Task.FromResult(Items.Values.Where(r => r.Active && r.IssuerSlug == null).Select(Clone).ToList());
    }

    private class FakeKeyService : IAsymmetricKeyService
    {
        private readonly Dictionary<string, (AsymmetricKey Key, AsymmetricCipherKeyPair Pair)> _keys = [];

        public Task SaveAsync(AsymmetricKey entity, string updatedBySlug)
        {
            var generator = new ECKeyPairGenerator();
            generator.Init(new ECKeyGenerationParameters(SecObjectIdentifiers.SecP256r1, new SecureRandom()));

            entity.Slug = Guid.NewGuid().ToString("N");
            _keys[entity.Slug] = (entity, generator.GenerateKeyPair());
            return Task.CompletedTask;
        }

        public Task<AsymmetricKey> GetAsync(string slug) => Task.FromResult(_keys.TryGetValue(slug, out var k) ? k.Key : null);

        public Task<AsymmetricCipherKeyPair> GetKeyPairAsync(string slug) => Task.FromResult(_keys.TryGetValue(slug, out var k) ? k.Pair : null);

        public Task<byte[]> GetPublicKey(string slug) => throw new NotSupportedException();

        public Task DeleteAsync(string slug, string updatedBySlug) => throw new NotSupportedException();

        public Task<List<AsymmetricKey>> GetAllAsync() => throw new NotSupportedException();

        public Task InitializeAsync() => Task.CompletedTask;
    }

    #endregion
}

internal static class X509CrlTestExtensions
{
    public static bool IsRevoked(this X509Crl crl, BigInteger serialNumber) =>
        crl.GetRevokedCertificate(serialNumber) != null;
}
