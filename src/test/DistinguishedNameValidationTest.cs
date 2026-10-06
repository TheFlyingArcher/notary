using Notary.Contract;

namespace Notary.Test;

public class DistinguishedNameValidationTest
{
    // ── CommonName ────────────────────────────────────────────────────────────

    [Test]
    public void Validate_ValidMinimalDn_DoesNotThrow()
    {
        var dn = new DistinguishedName { CommonName = "My Root CA" };
        Assert.DoesNotThrow(() => dn.Validate());
    }

    [Test]
    public void Validate_ValidFullDn_DoesNotThrow()
    {
        var dn = new DistinguishedName
        {
            CommonName = "My Issuing CA",
            Country = "US",
            StateProvince = "California",
            Locale = "San Francisco",
            Organization = "Acme Corp",
            OrganizationalUnit = "IT"
        };
        Assert.DoesNotThrow(() => dn.Validate());
    }

    [Test]
    public void Validate_NullCommonName_Throws()
    {
        var dn = new DistinguishedName { CommonName = null };
        var ex = Assert.Throws<ArgumentException>(() => dn.Validate());
        Assert.That(ex.ParamName, Is.EqualTo(nameof(DistinguishedName.CommonName)));
    }

    [Test]
    public void Validate_EmptyCommonName_Throws()
    {
        var dn = new DistinguishedName { CommonName = string.Empty };
        var ex = Assert.Throws<ArgumentException>(() => dn.Validate());
        Assert.That(ex.ParamName, Is.EqualTo(nameof(DistinguishedName.CommonName)));
    }

    [Test]
    public void Validate_WhitespaceCommonName_Throws()
    {
        var dn = new DistinguishedName { CommonName = "   " };
        var ex = Assert.Throws<ArgumentException>(() => dn.Validate());
        Assert.That(ex.ParamName, Is.EqualTo(nameof(DistinguishedName.CommonName)));
    }

    [Test]
    public void Validate_CommonNameExceeds64Chars_Throws()
    {
        var dn = new DistinguishedName { CommonName = new string('A', 65) };
        var ex = Assert.Throws<ArgumentException>(() => dn.Validate());
        Assert.That(ex.ParamName, Is.EqualTo(nameof(DistinguishedName.CommonName)));
    }

    [Test]
    public void Validate_CommonNameExactly64Chars_DoesNotThrow()
    {
        var dn = new DistinguishedName { CommonName = new string('A', 64) };
        Assert.DoesNotThrow(() => dn.Validate());
    }

    [Test]
    public void Validate_CommonNameWithNullByte_Throws()
    {
        var dn = new DistinguishedName { CommonName = "Test\0CA" };
        var ex = Assert.Throws<ArgumentException>(() => dn.Validate());
        Assert.That(ex.ParamName, Is.EqualTo(nameof(DistinguishedName.CommonName)));
    }

    [TestCase("<script>alert('xss')</script>")]
    [TestCase("CN=evil,O=attacker")]
    [TestCase("../../etc/passwd")]
    [TestCase("test\ninjection")]
    public void Validate_CommonNameWithInvalidChars_Throws(string commonName)
    {
        var dn = new DistinguishedName { CommonName = commonName };
        var ex = Assert.Throws<ArgumentException>(() => dn.Validate());
        Assert.That(ex.ParamName, Is.EqualTo(nameof(DistinguishedName.CommonName)));
    }

    // ── Country ───────────────────────────────────────────────────────────────

    [TestCase("US")]
    [TestCase("GB")]
    [TestCase("DE")]
    [TestCase("JP")]
    [TestCase("us")]
    public void Validate_ValidCountryCode_DoesNotThrow(string country)
    {
        var dn = new DistinguishedName { CommonName = "Test CA", Country = country };
        Assert.DoesNotThrow(() => dn.Validate());
    }

    [TestCase("USA")]
    [TestCase("1")]
    [TestCase("U1")]
    [TestCase("U S")]
    [TestCase("USAA")]
    public void Validate_InvalidCountryCode_Throws(string country)
    {
        var dn = new DistinguishedName { CommonName = "Test CA", Country = country };
        var ex = Assert.Throws<ArgumentException>(() => dn.Validate());
        Assert.That(ex.ParamName, Is.EqualTo(nameof(DistinguishedName.Country)));
    }

    [Test]
    public void Validate_NullCountry_DoesNotThrow()
    {
        var dn = new DistinguishedName { CommonName = "Test CA", Country = null };
        Assert.DoesNotThrow(() => dn.Validate());
    }

    // ── Optional components ───────────────────────────────────────────────────

    [Test]
    public void Validate_AllOptionalFieldsNull_DoesNotThrow()
    {
        var dn = new DistinguishedName
        {
            CommonName = "Minimal CA",
            Country = null,
            StateProvince = null,
            Locale = null,
            Organization = null,
            OrganizationalUnit = null
        };
        Assert.DoesNotThrow(() => dn.Validate());
    }

    [Test]
    public void Validate_OrganizationExceeds64Chars_Throws()
    {
        var dn = new DistinguishedName
        {
            CommonName = "Test CA",
            Organization = new string('X', 65)
        };
        var ex = Assert.Throws<ArgumentException>(() => dn.Validate());
        Assert.That(ex.ParamName, Is.EqualTo(nameof(DistinguishedName.Organization)));
    }

    [Test]
    public void Validate_LocaleWithNullByte_Throws()
    {
        var dn = new DistinguishedName { CommonName = "Test CA", Locale = "City\0Name" };
        var ex = Assert.Throws<ArgumentException>(() => dn.Validate());
        Assert.That(ex.ParamName, Is.EqualTo(nameof(DistinguishedName.Locale)));
    }

    [Test]
    public void Validate_StateProvinceWithInvalidChars_Throws()
    {
        var dn = new DistinguishedName { CommonName = "Test CA", StateProvince = "State<>Province" };
        var ex = Assert.Throws<ArgumentException>(() => dn.Validate());
        Assert.That(ex.ParamName, Is.EqualTo(nameof(DistinguishedName.StateProvince)));
    }

    [Test]
    public void Validate_OrganizationalUnitWithInvalidChars_Throws()
    {
        var dn = new DistinguishedName { CommonName = "Test CA", OrganizationalUnit = "IT;DROP TABLE" };
        var ex = Assert.Throws<ArgumentException>(() => dn.Validate());
        Assert.That(ex.ParamName, Is.EqualTo(nameof(DistinguishedName.OrganizationalUnit)));
    }

    // ── Allowed special characters ────────────────────────────────────────────

    [TestCase("My-CA")]
    [TestCase("my.ca.example")]
    [TestCase("CA_Root")]
    [TestCase("admin@example.com")]
    [TestCase("Server Auth CA")]
    public void Validate_CommonNameWithAllowedSpecialChars_DoesNotThrow(string commonName)
    {
        var dn = new DistinguishedName { CommonName = commonName };
        Assert.DoesNotThrow(() => dn.Validate());
    }
}
