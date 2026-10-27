using Notary.Security;

namespace Notary.Test;

public class KeyOptionsTest
{
    private static KeySpec Ec(EllipticCurve curve) => new(Algorithm.EllipticCurve, curve, null);

    private static KeySpec Rsa(int length) => new(Algorithm.RSA, null, length);

    [Test]
    public void NoIssuer_OffersEveryOption()
    {
        Assert.That(KeyOptions.AllowedRsaLengths(null), Is.EqualTo(new[] { 2048, 3072, 4096 }));
        Assert.That(KeyOptions.AllowedCurves(null), Is.EqualTo(new[] { EllipticCurve.P256, EllipticCurve.P384, EllipticCurve.P521 }));
    }

    [Test]
    public void P256Issuer_OffersRsaFromTheEqualStrengthUp_AndEveryCurve()
    {
        var issuer = Ec(EllipticCurve.P256);

        Assert.That(KeyOptions.AllowedRsaLengths(issuer), Is.EqualTo(new[] { 3072, 4096 }), "RSA-2048 (112) is weaker than 128");
        Assert.That(KeyOptions.AllowedCurves(issuer), Is.EqualTo(new[] { EllipticCurve.P256, EllipticCurve.P384, EllipticCurve.P521 }));
    }

    [Test]
    public void P384Issuer_OffersNoRsa_AndCurvesFromP384()
    {
        var issuer = Ec(EllipticCurve.P384);

        Assert.That(KeyOptions.AllowedRsaLengths(issuer), Is.Empty, "RSA-4096 is only 128-bit");
        Assert.That(KeyOptions.AllowedCurves(issuer), Is.EqualTo(new[] { EllipticCurve.P384, EllipticCurve.P521 }));
    }

    [Test]
    public void P521Issuer_OffersP521Only()
    {
        var issuer = Ec(EllipticCurve.P521);

        Assert.That(KeyOptions.AllowedRsaLengths(issuer), Is.Empty);
        Assert.That(KeyOptions.AllowedCurves(issuer), Is.EqualTo(new[] { EllipticCurve.P521 }));
    }

    [Test]
    public void Rsa2048Issuer_OffersEverything()
    {
        var issuer = Rsa(2048);

        Assert.That(KeyOptions.AllowedRsaLengths(issuer), Is.EqualTo(new[] { 2048, 3072, 4096 }));
        Assert.That(KeyOptions.AllowedCurves(issuer), Is.EqualTo(new[] { EllipticCurve.P256, EllipticCurve.P384, EllipticCurve.P521 }));
    }

    [Test]
    public void Rsa3072Issuer_IsEqualToP256()
    {
        var issuer = Rsa(3072);

        Assert.That(KeyOptions.AllowedRsaLengths(issuer), Is.EqualTo(new[] { 3072, 4096 }));
        Assert.That(KeyOptions.AllowedCurves(issuer), Is.EqualTo(new[] { EllipticCurve.P256, EllipticCurve.P384, EllipticCurve.P521 }));
    }

    [Test]
    public void UnreadableIssuer_OffersNothing()
    {
        var issuer = new KeySpec(Algorithm.RSA, null, null);

        Assert.That(KeyOptions.AllowedRsaLengths(issuer), Is.Empty);
        Assert.That(KeyOptions.AllowedCurves(issuer), Is.Empty);
        Assert.That(KeyOptions.Describe(issuer), Does.Contain("could not be read"));
    }

    [Test]
    public void Describe_NamesTheIssuersSecurityBits()
    {
        Assert.That(KeyOptions.Describe(Ec(EllipticCurve.P384)), Does.Contain("192-bit"));
    }

    [Test]
    public void Describe_ExplainsThatKeysAreComparedBySecurityLevel()
    {
        var note = KeyOptions.Describe(Rsa(4096));

        Assert.That(note, Does.Contain("security level, not raw size"));
        Assert.That(note, Does.Contain("RSA 3072 and RSA 4096 both give 128-bit security"));
    }

    [Test]
    public void Describe_OmitsTheRsaExampleAtOtherLevels()
    {
        Assert.That(KeyOptions.Describe(Ec(EllipticCurve.P384)), Does.Not.Contain("RSA 3072 and RSA 4096"));
        Assert.That(KeyOptions.Describe(Rsa(2048)), Does.Not.Contain("RSA 3072 and RSA 4096"));
    }

    [Test]
    public void Relate_Rsa4096Parent_Rsa3072IsTheSameSecurityLevel_NotWeaker()
    {
        var issuer = Rsa(4096);

        Assert.That(KeyOptions.Relate(issuer, Rsa(3072)), Is.EqualTo(KeyRelation.SameSecurityLevel));
        Assert.That(KeyOptions.Relate(issuer, Rsa(4096)), Is.EqualTo(KeyRelation.SameAsParent));
        Assert.That(KeyOptions.Relate(issuer, Ec(EllipticCurve.P256)), Is.EqualTo(KeyRelation.SameSecurityLevel));
        Assert.That(KeyOptions.Relate(issuer, Ec(EllipticCurve.P384)), Is.EqualTo(KeyRelation.Stronger));
    }

    [Test]
    public void Relate_P256Parent()
    {
        var issuer = Ec(EllipticCurve.P256);

        Assert.That(KeyOptions.Relate(issuer, Ec(EllipticCurve.P256)), Is.EqualTo(KeyRelation.SameAsParent));
        Assert.That(KeyOptions.Relate(issuer, Rsa(3072)), Is.EqualTo(KeyRelation.SameSecurityLevel));
        Assert.That(KeyOptions.Relate(issuer, Ec(EllipticCurve.P384)), Is.EqualTo(KeyRelation.Stronger));
    }

    [Test]
    public void Relate_P384Parent_P521IsStronger()
    {
        Assert.That(KeyOptions.Relate(Ec(EllipticCurve.P384), Ec(EllipticCurve.P521)), Is.EqualTo(KeyRelation.Stronger));
    }

    [Test]
    public void Relate_WeakerOrUnreadable_IsNull()
    {
        Assert.That(KeyOptions.Relate(Ec(EllipticCurve.P384), Ec(EllipticCurve.P256)), Is.Null);
        Assert.That(KeyOptions.Relate(Rsa(3072), Rsa(2048)), Is.Null);
        Assert.That(KeyOptions.Relate(Rsa(3072), new KeySpec(Algorithm.RSA, null, null)), Is.Null);
        Assert.That(KeyOptions.Relate(new KeySpec(Algorithm.RSA, null, null), Rsa(3072)), Is.Null);
    }

    [Test]
    public void Relate_EveryOfferedOptionHasARelation()
    {
        foreach (var issuer in new[] { Rsa(2048), Rsa(3072), Rsa(4096), Ec(EllipticCurve.P256), Ec(EllipticCurve.P384), Ec(EllipticCurve.P521) })
        {
            foreach (var length in KeyOptions.AllowedRsaLengths(issuer))
                Assert.That(KeyOptions.Relate(issuer, Rsa(length)), Is.Not.Null, $"RSA {length} under {KeyOptions.Label(issuer)}");

            foreach (var curve in KeyOptions.AllowedCurves(issuer))
                Assert.That(KeyOptions.Relate(issuer, Ec(curve)), Is.Not.Null, $"{curve} under {KeyOptions.Label(issuer)}");
        }
    }

    [TestCase(EllipticCurve.P256, "P-256 - 128-bit security")]
    [TestCase(EllipticCurve.P384, "P-384 - 192-bit security")]
    [TestCase(EllipticCurve.P521, "P-521 - 256-bit security")]
    public void Label_Ec_NamesTheCurveAndLevel(EllipticCurve curve, string expected)
    {
        Assert.That(KeyOptions.Label(Ec(curve)), Is.EqualTo(expected));
    }

    [TestCase(2048, "RSA 2048 bit - 112-bit security")]
    [TestCase(3072, "RSA 3072 bit - 128-bit security")]
    [TestCase(4096, "RSA 4096 bit - 128-bit security")]
    public void Label_Rsa_NamesTheSizeAndLevel(int length, string expected)
    {
        Assert.That(KeyOptions.Label(Rsa(length)), Is.EqualTo(expected));
    }

    [Test]
    public void RelationText_IsNeutral()
    {
        Assert.That(KeyOptions.RelationText(KeyRelation.SameAsParent), Is.EqualTo("Same as issuing CA"));
        Assert.That(KeyOptions.RelationText(KeyRelation.SameSecurityLevel), Is.EqualTo("Same security level"));
        Assert.That(KeyOptions.RelationText(KeyRelation.Stronger), Is.EqualTo("Stronger"));
        Assert.That(
            Enum.GetValues<KeyRelation>().Select(KeyOptions.RelationText),
            Has.None.Matches<string>(text => text.Contains("weak", StringComparison.OrdinalIgnoreCase)
                || text.Contains("smaller", StringComparison.OrdinalIgnoreCase)
                || text.Contains("downgrade", StringComparison.OrdinalIgnoreCase)));
    }

    [Test]
    public void DescribeIssuer_NamesTheParentKey()
    {
        Assert.That(KeyOptions.DescribeIssuer(Rsa(4096)), Is.EqualTo("Issuing CA key: RSA 4096 bit - 128-bit security"));
    }
}
