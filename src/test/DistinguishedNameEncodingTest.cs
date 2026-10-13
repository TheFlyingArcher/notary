using Notary.Contract;
using Org.BouncyCastle.Asn1.X509;

namespace Notary.Test;

/// <summary>
/// A distinguished name is assembled into a string and parsed again when a certificate is generated, so every
/// value must survive that round trip unchanged and never be able to add or alter another attribute.
/// </summary>
public class DistinguishedNameEncodingTest
{
    // RFC 5280 Appendix A upper bounds: common name, organization and organizational unit 64; locality and state 128
    private const int NameLimit = 64;
    private const int PlaceLimit = 128;

    [TestCase("Acme, Inc.")]
    [TestCase("Web CA (Production)")]
    [TestCase("O'Brien & Sons")]
    [TestCase("Say \"hello\"")]
    [TestCase("a+b")]
    [TestCase("a;b")]
    [TestCase("a<b>c")]
    [TestCase("a=b")]
    [TestCase(@"back\slash")]
    [TestCase(@"ends with backslash\")]
    [TestCase("#starts with hash")]
    [TestCase("a,")]
    [TestCase("Café Müller 東京")]
    [TestCase("CN=evil,O=attacker")]
    [TestCase("x,OU=injected")]
    [TestCase("x+CN=second")]
    [TestCase("<script>alert('xss')</script>")]
    [TestCase("../../etc/passwd")]
    public void CommonName_SurvivesTheStringRoundTrip(string commonName)
    {
        var dn = new DistinguishedName { CommonName = commonName, Country = "US", Organization = "Org" };

        Assert.That(dn.Check().IsSuccess, Is.True, dn.Check().Error);

        var name = new X509Name(DistinguishedName.BuildDistinguishedName(dn));

        Assert.That(name.GetValueList(X509Name.CN), Is.EqualTo(new[] { commonName }), "the value comes back unchanged");
        Assert.That(name.GetValueList(X509Name.O), Is.EqualTo(new[] { "Org" }), "no attribute was added or replaced");
        Assert.That(name.GetValueList(X509Name.OU), Is.Empty, "no attribute was injected");
        Assert.That(name.GetValueList(X509Name.C), Is.EqualTo(new[] { "US" }));
    }

    [Test]
    public void EveryComponent_SurvivesTheRoundTrip()
    {
        var dn = new DistinguishedName
        {
            CommonName = "Acme, Inc. Root",
            Country = "GB",
            Locale = "Newcastle, upon Tyne",
            Organization = "Acme; Ltd",
            OrganizationalUnit = "R&D (Labs)",
            StateProvince = "Tyne+Wear,"
        };

        var name = new X509Name(DistinguishedName.BuildDistinguishedName(dn));

        Assert.That(name.GetValueList(X509Name.CN), Is.EqualTo(new[] { dn.CommonName }));
        Assert.That(name.GetValueList(X509Name.L), Is.EqualTo(new[] { dn.Locale }));
        Assert.That(name.GetValueList(X509Name.O), Is.EqualTo(new[] { dn.Organization }));
        Assert.That(name.GetValueList(X509Name.OU), Is.EqualTo(new[] { dn.OrganizationalUnit }));
        Assert.That(name.GetValueList(X509Name.ST), Is.EqualTo(new[] { dn.StateProvince }), "a trailing comma is not trimmed");
    }

    [Test]
    public void Build_PlainNames_AreUnchanged()
    {
        var dn = new DistinguishedName { CommonName = "My Root CA", Country = "US", Organization = "Acme-Corp_1" };

        Assert.That(DistinguishedName.BuildDistinguishedName(dn), Is.EqualTo("CN=My Root CA,C=US,O=Acme-Corp_1"));
    }

    [TestCase(NameLimit, true)]
    [TestCase(NameLimit + 1, false)]
    public void CommonName_LengthLimitIs64(int length, bool valid)
    {
        Assert.That(new DistinguishedName { CommonName = new string('a', length) }.Check().IsSuccess, Is.EqualTo(valid));
    }

    [Test]
    public void CommonName_LengthCountsCharactersNotUtf16Units()
    {
        // 64 emoji are 128 UTF-16 units but 64 characters
        Assert.That(new DistinguishedName { CommonName = string.Concat(Enumerable.Repeat("😀", 64)) }.Check().IsSuccess, Is.True);
        Assert.That(new DistinguishedName { CommonName = string.Concat(Enumerable.Repeat("😀", 65)) }.Check().IsSuccess, Is.False);
    }

    [TestCase(PlaceLimit, true)]
    [TestCase(PlaceLimit + 1, false)]
    public void LocaleAndState_LengthLimitIs128(int length, bool valid)
    {
        Assert.That(new DistinguishedName { CommonName = "x", Locale = new string('a', length) }.Check().IsSuccess, Is.EqualTo(valid));
        Assert.That(new DistinguishedName { CommonName = "x", StateProvince = new string('a', length) }.Check().IsSuccess, Is.EqualTo(valid));
    }

    [TestCase(NameLimit, true)]
    [TestCase(NameLimit + 1, false)]
    public void OrganizationAndUnit_LengthLimitIs64(int length, bool valid)
    {
        Assert.That(new DistinguishedName { CommonName = "x", Organization = new string('a', length) }.Check().IsSuccess, Is.EqualTo(valid));
        Assert.That(new DistinguishedName { CommonName = "x", OrganizationalUnit = new string('a', length) }.Check().IsSuccess, Is.EqualTo(valid));
    }

    [TestCase("tab\there")]
    [TestCase("new\nline")]
    [TestCase("carriage\rreturn")]
    [TestCase("null\0byte")]
    [TestCase("bell\a")]
    public void CommonName_ControlInvisibleAndInvalidCharacters_AreRejected(string commonName)
    {
        var result = new DistinguishedName { CommonName = commonName }.Check();

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("Common Name"));
    }

    // Zero-width space, right-to-left override, line separator and a no-break space (which is allowed)
    [TestCase(0x200B, false)]
    [TestCase(0x202E, false)]
    [TestCase(0x2028, false)]
    [TestCase(0x00A0, true)]
    public void CommonName_InvisibleCharacters_AreRejectedByCodePoint(int codePoint, bool valid)
    {
        var commonName = "name" + char.ConvertFromUtf32(codePoint) + "here";

        Assert.That(new DistinguishedName { CommonName = commonName }.Check().IsSuccess, Is.EqualTo(valid));
    }

    [Test]
    public void CommonName_LoneSurrogate_IsRejected()
    {
        var commonName = "lone" + (char)0xD800 + "surrogate";

        var result = new DistinguishedName { CommonName = commonName }.Check();

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("not valid text"));
    }

    [TestCase(" leading space")]
    [TestCase("trailing space ")]
    [TestCase(" both ")]
    public void LeadingAndTrailingSpaces_AreRejectedWithAClearMessage(string commonName)
    {
        var result = new DistinguishedName { CommonName = commonName }.Check();

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("start or end with a space"));
    }

    [Test]
    public void Check_ReportsAProblemAsAResultInsteadOfThrowing()
    {
        var dn = new DistinguishedName { CommonName = "ok", Country = "USA" };

        Assert.DoesNotThrow(() => dn.Check());
        Assert.That(dn.Check().Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(dn.Check().Error, Does.Contain("Country"));
    }

    [Test]
    public void Check_EmptyCommonName_IsInvalid()
    {
        Assert.That(new DistinguishedName { CommonName = "  " }.Check().Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public void CheckCommonName_ReturnsAMessageOrNull()
    {
        Assert.That(DistinguishedName.CheckCommonName("Acme, Inc."), Is.Null);
        Assert.That(DistinguishedName.CheckCommonName(new string('a', 65)), Does.Contain("64"));
        Assert.That(DistinguishedName.CheckCommonName(""), Does.Contain("required"));
        Assert.That(DistinguishedName.CheckCommonName(null), Does.Contain("required"));
    }
}
