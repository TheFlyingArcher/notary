using Notary.Contract;
using Notary.Profiles;

namespace Notary.Test;

public class CaDetailInfoTest
{
    private static CertificateAuthority Ca(CaPurpose? purpose, string parentSlug = null, string slug = "ca")
    {
        return new CertificateAuthority { Slug = slug, Name = slug, Purpose = purpose, ParentCaSlug = parentSlug };
    }

    [TestCase(CaPurpose.Root)]
    [TestCase(CaPurpose.TlsSsl)]
    [TestCase(CaPurpose.Smime)]
    [TestCase(CaPurpose.UserLogin)]
    [TestCase(CaPurpose.IoT)]
    [TestCase(CaPurpose.Dot1xRadius)]
    [TestCase(CaPurpose.Custom)]
    public void KindName_WithPurpose_IsTheProfileDisplayName(CaPurpose purpose)
    {
        Assert.That(CaDetailInfo.KindName(Ca(purpose, "parent")), Is.EqualTo(CaProfileCatalog.Get(purpose).DisplayName));
    }

    [Test]
    public void KindName_NoPurpose_IsLegacy()
    {
        Assert.That(CaDetailInfo.KindName(Ca(null, "parent")), Is.EqualTo("Legacy"));
    }

    [Test]
    public void IsRoot_RootPurpose_IsTrue()
    {
        Assert.That(CaDetailInfo.IsRoot(Ca(CaPurpose.Root)), Is.True);
    }

    [Test]
    public void IsRoot_RootPurposeWithAParent_IsTrue()
    {
        Assert.That(CaDetailInfo.IsRoot(Ca(CaPurpose.Root, "parent")), Is.True);
    }

    [Test]
    public void IsRoot_LegacyCaWithoutParent_IsTrue()
    {
        Assert.That(CaDetailInfo.IsRoot(Ca(null)), Is.True);
    }

    [Test]
    public void IsRoot_EmptyParent_IsTrue()
    {
        Assert.That(CaDetailInfo.IsRoot(Ca(CaPurpose.TlsSsl, "")), Is.True);
    }

    [Test]
    public void IsRoot_SelfParent_IsTrue()
    {
        Assert.That(CaDetailInfo.IsRoot(Ca(null, "ca", "ca")), Is.True);
    }

    [TestCase(CaPurpose.TlsSsl)]
    [TestCase(CaPurpose.Custom)]
    [TestCase(null)]
    public void IsRoot_ChildOfAnotherCa_IsFalse(CaPurpose? purpose)
    {
        Assert.That(CaDetailInfo.IsRoot(Ca(purpose, "parent")), Is.False);
    }
}
