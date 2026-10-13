using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using Notary.Contract;
using Notary.Data;
using Notary.Data.Model;
using Notary.Profiles;

namespace Notary.Test;

public class CaPurposePersistenceTest
{
    private IMapper _mapper;

    [SetUp]
    public void SetUp()
    {
        var config = new MapperConfiguration(c => c.AddProfile<ModelMapProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
    }

    [Test]
    public void Mapping_PurposeRoundTrips()
    {
        var ca = NewCa();
        ca.Purpose = CaPurpose.TlsSsl;

        var back = _mapper.Map<CertificateAuthority>(_mapper.Map<CertificateAuthorityModel>(ca));

        Assert.That(back.Purpose, Is.EqualTo(CaPurpose.TlsSsl));
    }

    [Test]
    public void Mapping_CustomUsagesRoundTrip()
    {
        var ca = NewCa();
        ca.Purpose = CaPurpose.Custom;
        ca.CustomKeyUsages = [CertificateKeyUsage.DigitalSignature, CertificateKeyUsage.KeyCertSign];
        ca.CustomExtendedKeyUsages = [KeyPurposeOids.ServerAuthentication, KeyPurposeOids.EmailProtection];

        var model = _mapper.Map<CertificateAuthorityModel>(ca);
        var back = _mapper.Map<CertificateAuthority>(model);

        Assert.That(model.CustomKeyUsages, Is.EqualTo(new[] { "DigitalSignature", "KeyCertSign" }));
        Assert.That(back.CustomKeyUsages, Is.EqualTo(ca.CustomKeyUsages));
        Assert.That(back.CustomExtendedKeyUsages, Is.EqualTo(ca.CustomExtendedKeyUsages));
    }

    [Test]
    public void Mapping_NullPurposeStaysNull_AndResolvesToCustom()
    {
        var ca = NewCa();

        var back = _mapper.Map<CertificateAuthority>(_mapper.Map<CertificateAuthorityModel>(ca));

        Assert.That(back.Purpose, Is.Null);
        Assert.That(back.CustomKeyUsages, Is.Null);
        Assert.That(CaProfileCatalog.Get(back.Purpose).Purpose, Is.EqualTo(CaPurpose.Custom));
    }

    [Test]
    public void Bson_PurposeIsStoredAsAString()
    {
        var model = _mapper.Map<CertificateAuthorityModel>(WithPurpose(CaPurpose.Dot1xRadius));

        var document = model.ToBsonDocument();

        Assert.That(document["purpose"], Is.EqualTo(new BsonString("Dot1xRadius")));
    }

    [Test]
    public void Bson_UnsetValuesWriteNoElements()
    {
        var model = _mapper.Map<CertificateAuthorityModel>(NewCa());

        var document = model.ToBsonDocument();

        Assert.That(document.Contains("purpose"), Is.False);
        Assert.That(document.Contains("custom_key_usages"), Is.False);
        Assert.That(document.Contains("custom_ex_key_usages"), Is.False);
    }

    [Test]
    public void Bson_CustomUsagesRoundTrip()
    {
        var ca = WithPurpose(CaPurpose.Custom);
        ca.CustomKeyUsages = [CertificateKeyUsage.DigitalSignature];
        ca.CustomExtendedKeyUsages = [KeyPurposeOids.ClientAuthentication];

        var document = _mapper.Map<CertificateAuthorityModel>(ca).ToBsonDocument();
        var back = _mapper.Map<CertificateAuthority>(BsonSerializer.Deserialize<CertificateAuthorityModel>(document));

        Assert.That(back.Purpose, Is.EqualTo(CaPurpose.Custom));
        Assert.That(back.CustomKeyUsages, Is.EqualTo(new[] { CertificateKeyUsage.DigitalSignature }));
        Assert.That(back.CustomExtendedKeyUsages, Is.EqualTo(new[] { KeyPurposeOids.ClientAuthentication }));
    }

    [Test]
    public void Bson_LegacyDocumentWithoutPurposeLoadsAsCustom()
    {
        // A document written before purposes existed has none of the new elements
        var document = _mapper.Map<CertificateAuthorityModel>(WithPurpose(CaPurpose.TlsSsl)).ToBsonDocument();
        document.Remove("purpose");
        document.Remove("custom_key_usages");
        document.Remove("custom_ex_key_usages");

        var back = _mapper.Map<CertificateAuthority>(BsonSerializer.Deserialize<CertificateAuthorityModel>(document));

        Assert.That(back.Purpose, Is.Null);
        Assert.That(CaProfileCatalog.Get(back.Purpose).Purpose, Is.EqualTo(CaPurpose.Custom));
    }

    [Test]
    public void Json_PurposeIsWrittenByName()
    {
        var json = JsonConvert.SerializeObject(WithPurpose(CaPurpose.Smime));

        Assert.That(json, Does.Contain("\"purpose\":\"Smime\""));
    }

    [Test]
    public void Json_UnsetPurposeIsOmitted_AndOldPayloadsStillParse()
    {
        var json = JsonConvert.SerializeObject(NewCa());

        Assert.That(json, Does.Not.Contain("purpose"));
        Assert.That(json, Does.Not.Contain("customKu"));

        var parsed = JsonConvert.DeserializeObject<CertificateAuthority>(json);
        Assert.That(parsed.Purpose, Is.Null);
    }

    private static CertificateAuthority WithPurpose(CaPurpose purpose)
    {
        var ca = NewCa();
        ca.Purpose = purpose;
        return ca;
    }

    private static CertificateAuthority NewCa()
    {
        return new CertificateAuthority
        {
            Active = true,
            Created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedBySlug = "tester",
            DistinguishedName = new DistinguishedName { CommonName = "Test CA", Country = "US" },
            IsIssuer = true,
            IssuingDn = new DistinguishedName { CommonName = "Parent CA", Country = "US" },
            ParentCaSlug = "parent-ca",
            KeyAlgorithm = Algorithm.EllipticCurve,
            KeyCurve = EllipticCurve.P256,
            Name = "test-ca",
            NotAfter = new DateTime(2036, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            NotBefore = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Slug = "test-ca",
            CertificateSlug = "test-ca-cert"
        };
    }
}
