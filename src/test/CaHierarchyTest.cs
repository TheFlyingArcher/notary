using Notary.Contract;

namespace Notary.Test;

public class CaHierarchyTest
{
    private static CertificateAuthority Ca(string slug, string name, string parentSlug = null)
    {
        return new CertificateAuthority { Slug = slug, Name = name, ParentCaSlug = parentSlug };
    }

    private static string[] Order(IEnumerable<CertificateAuthority> authorities)
    {
        return CaHierarchy.Flatten(authorities).Select(r => r.Authority.Slug).ToArray();
    }

    [Test]
    public void Flatten_EmptyInput_ReturnsEmpty()
    {
        Assert.That(CaHierarchy.Flatten([]), Is.Empty);
    }

    [Test]
    public void Flatten_IsDepthFirstWithChildrenUnderParent()
    {
        var cas = new[]
        {
            Ca("tls", "tls", "root-a"),
            Ca("root-b", "Beta"),
            Ca("root-a", "Alpha"),
            Ca("mail", "mail", "root-a"),
            Ca("b-child", "child", "root-b")
        };

        Assert.That(Order(cas), Is.EqualTo(new[] { "root-a", "mail", "tls", "root-b", "b-child" }));
    }

    [Test]
    public void Flatten_SortsRootsAndSiblingsByNameIgnoringCase()
    {
        var cas = new[]
        {
            Ca("z", "zulu"),
            Ca("a", "Alpha"),
            Ca("c2", "charlie", "a"),
            Ca("c1", "Bravo", "a")
        };

        Assert.That(Order(cas), Is.EqualTo(new[] { "a", "c1", "c2", "z" }));
    }

    [Test]
    public void Flatten_MultiLevelChain_IncreasesDepth()
    {
        var cas = new[]
        {
            Ca("leaf", "c", "mid"),
            Ca("mid", "b", "root"),
            Ca("root", "a")
        };

        var rows = CaHierarchy.Flatten(cas);

        Assert.That(rows.Select(r => (r.Authority.Slug, r.Depth)),
            Is.EqualTo(new[] { ("root", 0), ("mid", 1), ("leaf", 2) }));
    }

    [Test]
    public void Flatten_MissingParent_TreatedAsRoot()
    {
        var cas = new[] { Ca("orphan", "orphan", "gone"), Ca("root", "root") };

        var rows = CaHierarchy.Flatten(cas);

        Assert.That(rows.Select(r => (r.Authority.Slug, r.Depth)), Is.EqualTo(new[] { ("orphan", 0), ("root", 0) }));
    }

    [Test]
    public void Flatten_EmptyParentSlug_TreatedAsRoot()
    {
        var cas = new[] { Ca("a", "a", ""), Ca("b", "b", null) };

        Assert.That(CaHierarchy.Flatten(cas).All(r => r.Depth == 0), Is.True);
    }

    [Test]
    public void Flatten_SelfParent_DoesNotLoopAndShowsCa()
    {
        var cas = new[] { Ca("a", "a", "a") };

        Assert.That(Order(cas), Is.EqualTo(new[] { "a" }));
    }

    [Test]
    public void Flatten_ParentCycle_TerminatesAndShowsEveryCa()
    {
        var cas = new[] { Ca("a", "a", "b"), Ca("b", "b", "a"), Ca("root", "root") };

        var order = Order(cas);

        Assert.That(order, Is.EquivalentTo(new[] { "a", "b", "root" }));
        Assert.That(order, Has.Length.EqualTo(3));
    }
}
