namespace Notary.Test;

public class CertificateStatusTest
{
    private static readonly DateTime Now = new(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc);

    [Test]
    public void Get_FarFromExpiry_IsValid()
    {
        Assert.That(CertificateStatusCalculator.Get(Now.AddDays(90), null, Now), Is.EqualTo(CertificateStatus.Valid));
    }

    [Test]
    public void Get_ExactlyThirtyDaysLeft_IsValid()
    {
        Assert.That(CertificateStatusCalculator.Get(Now.AddDays(30), null, Now), Is.EqualTo(CertificateStatus.Valid));
    }

    [Test]
    public void Get_WithinThirtyDays_IsExpiring()
    {
        Assert.That(CertificateStatusCalculator.Get(Now.AddDays(29), null, Now), Is.EqualTo(CertificateStatus.Expiring));
    }

    [Test]
    public void Get_PastNotAfter_IsExpired()
    {
        Assert.That(CertificateStatusCalculator.Get(Now.AddSeconds(-1), null, Now), Is.EqualTo(CertificateStatus.Expired));
    }

    [Test]
    public void Get_RevokedAndFarFromExpiry_IsRevoked()
    {
        Assert.That(CertificateStatusCalculator.Get(Now.AddDays(90), Now.AddDays(-1), Now), Is.EqualTo(CertificateStatus.Revoked));
    }

    [Test]
    public void Get_RevokedAndExpired_IsExpired()
    {
        Assert.That(CertificateStatusCalculator.Get(Now.AddDays(-5), Now.AddDays(-10), Now), Is.EqualTo(CertificateStatus.Expired));
    }

    [Test]
    public void Get_RevokedAndExpiring_IsExpiring()
    {
        Assert.That(CertificateStatusCalculator.Get(Now.AddDays(5), Now.AddDays(-1), Now), Is.EqualTo(CertificateStatus.Expiring));
    }
}
