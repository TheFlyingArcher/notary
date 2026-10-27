using Notary.Security;

namespace Notary.Test;

public class KeyStrengthTest
{
    private static KeySpec Ec(EllipticCurve? curve) => new(Algorithm.EllipticCurve, curve, null);

    private static KeySpec Rsa(int? length) => new(Algorithm.RSA, null, length);

    [TestCase(EllipticCurve.P256, 128)]
    [TestCase(EllipticCurve.P384, 192)]
    [TestCase(EllipticCurve.P521, 256)]
    public void GetSecurityBits_EcCurve_FollowsNist(EllipticCurve curve, int expected)
    {
        var result = KeyStrength.GetSecurityBits(Ec(curve));

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(result.Value, Is.EqualTo(expected));
    }

    [TestCase(1, 80)]
    [TestCase(1024, 80)]
    [TestCase(2047, 80)]
    [TestCase(2048, 112)]
    [TestCase(2560, 112)]
    [TestCase(3071, 112)]
    [TestCase(3072, 128)]
    [TestCase(4096, 128)]
    [TestCase(7679, 128)]
    [TestCase(7680, 192)]
    [TestCase(15359, 192)]
    [TestCase(15360, 256)]
    [TestCase(16384, 256)]
    public void GetSecurityBits_RsaLength_TakesTheNistRowAtOrBelow(int length, int expected)
    {
        var result = KeyStrength.GetSecurityBits(Rsa(length));

        Assert.That(result.IsSuccess, Is.True, result.Error);
        Assert.That(result.Value, Is.EqualTo(expected));
    }

    [Test]
    public void GetSecurityBits_EcWithoutCurve_Fails()
    {
        var result = KeyStrength.GetSecurityBits(Ec(null));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [TestCase(null)]
    [TestCase(0)]
    [TestCase(-2048)]
    public void GetSecurityBits_RsaWithoutAUsableLength_Fails(int? length)
    {
        var result = KeyStrength.GetSecurityBits(Rsa(length));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public void GetSecurityBits_NullKey_Fails()
    {
        Assert.That(KeyStrength.GetSecurityBits(null).IsSuccess, Is.False);
    }

    [TestCase(EllipticCurve.P256, 3072)]
    [TestCase(EllipticCurve.P384, 7680)]
    [TestCase(EllipticCurve.P521, 15360)]
    public void CheckNotWeaker_EqualStrengthAcrossTypes_Passes(EllipticCurve curve, int rsaLength)
    {
        Assert.That(KeyStrength.CheckNotWeaker(Ec(curve), Rsa(rsaLength)).IsSuccess, Is.True, "RSA under EC");
        Assert.That(KeyStrength.CheckNotWeaker(Rsa(rsaLength), Ec(curve)).IsSuccess, Is.True, "EC under RSA");
    }

    [Test]
    public void CheckNotWeaker_StrongerSubjectAcrossTypes_Passes()
    {
        Assert.That(KeyStrength.CheckNotWeaker(Rsa(2048), Ec(EllipticCurve.P256)).IsSuccess, Is.True, "P-256 (128) under RSA-2048 (112)");
        Assert.That(KeyStrength.CheckNotWeaker(Ec(EllipticCurve.P256), Rsa(7680)).IsSuccess, Is.True, "RSA-7680 (192) under P-256 (128)");
    }

    [Test]
    public void CheckNotWeaker_SameTypeSteps()
    {
        Assert.That(KeyStrength.CheckNotWeaker(Ec(EllipticCurve.P256), Ec(EllipticCurve.P384)).IsSuccess, Is.True);
        Assert.That(KeyStrength.CheckNotWeaker(Ec(EllipticCurve.P384), Ec(EllipticCurve.P384)).IsSuccess, Is.True);
        Assert.That(KeyStrength.CheckNotWeaker(Rsa(3072), Rsa(4096)).IsSuccess, Is.True, "Same row");
        Assert.That(KeyStrength.CheckNotWeaker(Rsa(2048), Rsa(3072)).IsSuccess, Is.True);
    }

    [TestCase(EllipticCurve.P384, EllipticCurve.P256)]
    [TestCase(EllipticCurve.P521, EllipticCurve.P384)]
    public void CheckNotWeaker_WeakerEc_FailsWithBothStrengths(EllipticCurve issuerCurve, EllipticCurve subjectCurve)
    {
        var result = KeyStrength.CheckNotWeaker(Ec(issuerCurve), Ec(subjectCurve));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain($"{KeyStrength.GetSecurityBits(Ec(subjectCurve)).Value}-bit"));
        Assert.That(result.Error, Does.Contain($"{KeyStrength.GetSecurityBits(Ec(issuerCurve)).Value}-bit"));
    }

    [Test]
    public void CheckNotWeaker_WeakerAcrossTypes_Fails()
    {
        Assert.That(KeyStrength.CheckNotWeaker(Ec(EllipticCurve.P256), Rsa(2048)).IsSuccess, Is.False, "RSA-2048 under P-256");
        Assert.That(KeyStrength.CheckNotWeaker(Rsa(3072), Ec(EllipticCurve.P256)).IsSuccess, Is.True, "Equal at 128 bits");
        Assert.That(KeyStrength.CheckNotWeaker(Rsa(7680), Ec(EllipticCurve.P256)).IsSuccess, Is.False, "P-256 under RSA-7680");
        Assert.That(KeyStrength.CheckNotWeaker(Rsa(3072), Rsa(2560)).IsSuccess, Is.False, "RSA-2560 (112) under RSA-3072 (128)");
    }

    [Test]
    public void CheckNotWeaker_UnreadableKey_FailsOnEitherSide()
    {
        Assert.That(KeyStrength.CheckNotWeaker(Ec(null), Ec(EllipticCurve.P256)).IsSuccess, Is.False);
        Assert.That(KeyStrength.CheckNotWeaker(Ec(EllipticCurve.P256), Rsa(null)).IsSuccess, Is.False);
    }

    [Test]
    public void MeetsFloor_MatchesCheckNotWeaker()
    {
        Assert.That(KeyStrength.MeetsFloor(Ec(EllipticCurve.P384), Ec(EllipticCurve.P384)), Is.True);
        Assert.That(KeyStrength.MeetsFloor(Ec(EllipticCurve.P384), Ec(EllipticCurve.P256)), Is.False);
        Assert.That(KeyStrength.MeetsFloor(Ec(EllipticCurve.P384), Rsa(null)), Is.False);
    }
}
