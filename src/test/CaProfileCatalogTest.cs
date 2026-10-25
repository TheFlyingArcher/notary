using Notary.Profiles;

namespace Notary.Test;

public class CaProfileCatalogTest
{
    private const string ServerAuth = KeyPurposeOids.ServerAuthentication;
    private const string ClientAuth = KeyPurposeOids.ClientAuthentication;
    private const string Email = KeyPurposeOids.EmailProtection;
    private const string SmartCard = KeyPurposeOids.SmartCardLogin;

    private static Notary.Contract.CertificateAuthority Ca(CaPurpose? purpose, string parentSlug = null, string name = "test-ca")
    {
        return new Notary.Contract.CertificateAuthority
        {
            Name = name,
            ParentCaSlug = parentSlug,
            Purpose = purpose
        };
    }

    [Test]
    public void GetAll_ReturnsSevenPurposesInUserOrder()
    {
        var purposes = CaProfileCatalog.GetAll().Select(p => p.Purpose);

        Assert.That(purposes, Is.EqualTo(new[]
        {
            CaPurpose.Root,
            CaPurpose.TlsSsl,
            CaPurpose.UserLogin,
            CaPurpose.Smime,
            CaPurpose.IoT,
            CaPurpose.Dot1xRadius,
            CaPurpose.Custom
        }));
    }

    [Test]
    public void Get_NullPurpose_ResolvesToCustom()
    {
        Assert.That(CaProfileCatalog.Get(null).Purpose, Is.EqualTo(CaPurpose.Custom));
    }

    [Test]
    public void OnlyCustomIsDiscouraged()
    {
        var discouraged = CaProfileCatalog.GetAll().Where(p => p.IsDiscouraged).Select(p => p.Purpose);

        Assert.That(discouraged, Is.EqualTo(new[] { CaPurpose.Custom }));
    }

    [Test]
    public void EveryCaCarriesOnlyCertSignAndCrlSign()
    {
        foreach (var profile in CaProfileCatalog.GetAll())
        {
            Assert.That(
                profile.CaKeyUsages,
                Is.EquivalentTo(new[] { CertificateKeyUsage.KeyCertSign, CertificateKeyUsage.CrlSign }),
                profile.DisplayName);
        }
    }

    [TestCase(CaPurpose.Root, new string[0])]
    [TestCase(CaPurpose.TlsSsl, new[] { ServerAuth })]
    [TestCase(CaPurpose.UserLogin, new[] { ClientAuth, SmartCard })]
    [TestCase(CaPurpose.Smime, new[] { Email })]
    [TestCase(CaPurpose.IoT, new[] { ClientAuth, ServerAuth })]
    [TestCase(CaPurpose.Dot1xRadius, new[] { ClientAuth, ServerAuth })]
    [TestCase(CaPurpose.Custom, new string[0])]
    public void CaExtendedKeyUsages_MatchApprovedProfile(CaPurpose purpose, string[] expected)
    {
        Assert.That(CaProfileCatalog.Get(purpose).CaExtendedKeyUsages, Is.EquivalentTo(expected));
    }

    [TestCase(CaPurpose.TlsSsl, new[] { ServerAuth })]
    [TestCase(CaPurpose.UserLogin, new[] { ClientAuth, SmartCard })]
    [TestCase(CaPurpose.Smime, new[] { Email })]
    [TestCase(CaPurpose.IoT, new[] { ClientAuth, ServerAuth })]
    [TestCase(CaPurpose.Dot1xRadius, new[] { ClientAuth, ServerAuth })]
    public void LeafExtendedKeyUsages_MatchTheCaExtendedKeyUsages(CaPurpose purpose, string[] expected)
    {
        var profile = CaProfileCatalog.Get(purpose);

        Assert.That(profile.LeafExtendedKeyUsages, Is.EquivalentTo(expected));
        Assert.That(profile.RestrictsLeafUsages, Is.True);
    }

    [Test]
    public void LeafKeyUsages_MatchApprovedProfile()
    {
        var signAndEncipher = new[] { CertificateKeyUsage.DigitalSignature, CertificateKeyUsage.KeyEncipherment };

        foreach (var purpose in new[] { CaPurpose.TlsSsl, CaPurpose.UserLogin, CaPurpose.IoT, CaPurpose.Dot1xRadius })
        {
            Assert.That(CaProfileCatalog.Get(purpose).LeafKeyUsages, Is.EquivalentTo(signAndEncipher), purpose.ToString());
        }

        Assert.That(
            CaProfileCatalog.Get(CaPurpose.Smime).LeafKeyUsages,
            Is.EquivalentTo(new[]
            {
                CertificateKeyUsage.DigitalSignature,
                CertificateKeyUsage.NonRepudiation,
                CertificateKeyUsage.KeyEncipherment
            }));
    }

    [Test]
    public void RootNeverIssuesLeafCertificates_AndOtherPurposesDo()
    {
        Assert.That(CaProfileCatalog.Get(CaPurpose.Root).IssuesLeafCertificates, Is.False);
        Assert.That(CaProfileCatalog.Get(CaPurpose.Root).IssuesCaCertificates, Is.True);

        foreach (var profile in CaProfileCatalog.GetAll().Where(p => p.Purpose != CaPurpose.Root))
        {
            Assert.That(profile.IssuesLeafCertificates, Is.True, profile.DisplayName);
        }
    }

    [Test]
    public void CustomLeavesAreUnrestricted()
    {
        Assert.That(CaProfileCatalog.Get(CaPurpose.Custom).RestrictsLeafUsages, Is.False);
    }

    [Test]
    public void CreateCustom_AddsCertSignAndCrlSignAndRemovesDuplicates()
    {
        var profile = CaProfileCatalog.CreateCustom(
            [CertificateKeyUsage.DigitalSignature, CertificateKeyUsage.KeyCertSign, CertificateKeyUsage.DigitalSignature],
            [ServerAuth, ServerAuth]);

        Assert.That(profile.Purpose, Is.EqualTo(CaPurpose.Custom));
        Assert.That(
            profile.CaKeyUsages,
            Is.EquivalentTo(new[] { CertificateKeyUsage.DigitalSignature, CertificateKeyUsage.KeyCertSign, CertificateKeyUsage.CrlSign }));
        Assert.That(profile.CaExtendedKeyUsages, Is.EqualTo(new[] { ServerAuth }));
    }

    [Test]
    public void CreateCustom_NothingChosen_StillHasCertSignAndCrlSign()
    {
        var profile = CaProfileCatalog.CreateCustom([], []);

        Assert.That(profile.CaKeyUsages, Is.EquivalentTo(new[] { CertificateKeyUsage.KeyCertSign, CertificateKeyUsage.CrlSign }));
        Assert.That(profile.RestrictsExtendedKeyUsages, Is.False);
    }

    [TestCase(CaPurpose.Root, true, null)]
    [TestCase(CaPurpose.Root, false, null)]
    [TestCase(CaPurpose.Custom, true, null)]
    [TestCase(CaPurpose.Custom, false, null)]
    [TestCase(CaPurpose.TlsSsl, true, 0)]
    [TestCase(CaPurpose.TlsSsl, false, null)]
    [TestCase(CaPurpose.UserLogin, true, 0)]
    [TestCase(CaPurpose.Smime, true, 0)]
    [TestCase(CaPurpose.IoT, true, 0)]
    [TestCase(CaPurpose.Dot1xRadius, true, 0)]
    [TestCase(CaPurpose.Dot1xRadius, false, null)]
    public void GetPathLength_FollowsTheRule(CaPurpose purpose, bool hasParent, int? expected)
    {
        Assert.That(CaProfileCatalog.GetPathLength(purpose, hasParent), Is.EqualTo(expected));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void GetPathLength_NullPurpose_IsUnconstrained(bool hasParent)
    {
        Assert.That(CaProfileCatalog.GetPathLength(null, hasParent), Is.Null);
    }

    [Test]
    public void CheckChild_RootAsChild_IsInvalid()
    {
        var result = CaProfileCatalog.CheckChild(CaProfileCatalog.Get(CaPurpose.Root), CaProfileCatalog.Get(CaPurpose.Root));

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("Root"));
    }

    [TestCase(CaPurpose.Root, CaPurpose.TlsSsl)]
    [TestCase(CaPurpose.Root, CaPurpose.UserLogin)]
    [TestCase(CaPurpose.Root, CaPurpose.Smime)]
    [TestCase(CaPurpose.Root, CaPurpose.IoT)]
    [TestCase(CaPurpose.TlsSsl, CaPurpose.TlsSsl)]
    [TestCase(CaPurpose.IoT, CaPurpose.TlsSsl)]
    [TestCase(CaPurpose.IoT, CaPurpose.Dot1xRadius)]
    [TestCase(CaPurpose.Dot1xRadius, CaPurpose.IoT)]
    [TestCase(CaPurpose.Custom, CaPurpose.TlsSsl)]
    public void CheckChild_ChildWithinParent_Succeeds(CaPurpose parent, CaPurpose child)
    {
        var result = CaProfileCatalog.CheckChild(CaProfileCatalog.Get(parent), CaProfileCatalog.Get(child));

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [TestCase(CaPurpose.TlsSsl, CaPurpose.Smime)]
    [TestCase(CaPurpose.TlsSsl, CaPurpose.UserLogin)]
    [TestCase(CaPurpose.TlsSsl, CaPurpose.IoT)]
    [TestCase(CaPurpose.IoT, CaPurpose.UserLogin)]
    [TestCase(CaPurpose.Smime, CaPurpose.TlsSsl)]
    public void CheckChild_ChildOutsideParent_IsInvalidAndNamesTheUsage(CaPurpose parent, CaPurpose child)
    {
        var result = CaProfileCatalog.CheckChild(CaProfileCatalog.Get(parent), CaProfileCatalog.Get(child));

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("not permitted by the parent"));
    }

    [Test]
    public void CheckChild_CustomWithoutUsagesUnderRestrictedParent_IsInvalid()
    {
        var custom = CaProfileCatalog.CreateCustom([], []);

        var result = CaProfileCatalog.CheckChild(CaProfileCatalog.Get(CaPurpose.TlsSsl), custom);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("explicit usages"));
    }

    [Test]
    public void CheckChild_CustomWithinParent_Succeeds()
    {
        var custom = CaProfileCatalog.CreateCustom([], [ServerAuth]);

        var result = CaProfileCatalog.CheckChild(CaProfileCatalog.Get(CaPurpose.TlsSsl), custom);

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [Test]
    public void CheckChild_CustomOutsideParent_IsInvalid()
    {
        var custom = CaProfileCatalog.CreateCustom([], [ServerAuth, Email]);

        var result = CaProfileCatalog.CheckChild(CaProfileCatalog.Get(CaPurpose.TlsSsl), custom);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("E-mail Protection"));
    }

    [Test]
    public void CheckChild_UnrestrictedCustomParent_AcceptsAnything()
    {
        var parent = CaProfileCatalog.Get(CaPurpose.Custom);

        foreach (var child in CaProfileCatalog.GetAll().Where(p => p.Purpose != CaPurpose.Root))
        {
            Assert.That(CaProfileCatalog.CheckChild(parent, child).IsSuccess, Is.True, child.DisplayName);
        }
    }

    [Test]
    public void CheckChild_RestrictedCustomParent_LimitsChildren()
    {
        var parent = CaProfileCatalog.CreateCustom([], [ServerAuth]);

        Assert.That(CaProfileCatalog.CheckChild(parent, CaProfileCatalog.Get(CaPurpose.TlsSsl)).IsSuccess, Is.True);
        Assert.That(CaProfileCatalog.CheckChild(parent, CaProfileCatalog.Get(CaPurpose.Smime)).Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public void Resolve_NoPurpose_IsUnrestrictedCustom()
    {
        var profile = CaProfileCatalog.Resolve(Ca(null));

        Assert.That(profile.Purpose, Is.EqualTo(CaPurpose.Custom));
        Assert.That(profile.RestrictsExtendedKeyUsages, Is.False);
    }

    [Test]
    public void Resolve_NamedPurpose_IsItsCatalogProfile()
    {
        Assert.That(CaProfileCatalog.Resolve(Ca(CaPurpose.Smime)), Is.EqualTo(CaProfileCatalog.Get(CaPurpose.Smime)));
    }

    [Test]
    public void Resolve_CustomWithChosenUsages_UsesThemPlusCertSignAndCrlSign()
    {
        var ca = Ca(CaPurpose.Custom);
        ca.CustomKeyUsages = [CertificateKeyUsage.DigitalSignature];
        ca.CustomExtendedKeyUsages = [ServerAuth];

        var profile = CaProfileCatalog.Resolve(ca);

        Assert.That(profile.CaExtendedKeyUsages, Is.EqualTo(new[] { ServerAuth }));
        Assert.That(
            profile.CaKeyUsages,
            Is.EquivalentTo(new[] { CertificateKeyUsage.DigitalSignature, CertificateKeyUsage.KeyCertSign, CertificateKeyUsage.CrlSign }));
    }

    [Test]
    public void Resolve_CustomWithNothingChosen_IsUnrestricted()
    {
        Assert.That(CaProfileCatalog.Resolve(Ca(CaPurpose.Custom)).RestrictsExtendedKeyUsages, Is.False);
    }

    [TestCase(CaPurpose.Root, false)]
    [TestCase(CaPurpose.TlsSsl, false)]
    [TestCase(null, false)]
    [TestCase(null, true)]
    [TestCase(CaPurpose.Custom, true)]
    [TestCase(CaPurpose.Root, true)]
    public void CheckCanIssueCas_AllowedWhenNotAPurposeCaWithAParent(CaPurpose? purpose, bool hasParent)
    {
        Assert.That(CaProfileCatalog.CheckCanIssueCas(Ca(purpose, hasParent ? "some-parent" : null)).IsSuccess, Is.True);
    }

    [Test]
    public void CheckCanIssueCas_PurposeCaWithAParent_IsInvalid()
    {
        var result = CaProfileCatalog.CheckCanIssueCas(Ca(CaPurpose.TlsSsl, "root", "web-ca"));

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("web-ca").And.Contain("end-entity"));
    }

    [Test]
    public void CheckIssuer_ParentWithPathLengthZero_RejectsEvenTheSamePurpose()
    {
        var parent = Ca(CaPurpose.TlsSsl, "root");

        var result = CaProfileCatalog.CheckIssuer(parent, CaProfileCatalog.Get(CaPurpose.TlsSsl));

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("end-entity"));
    }

    [Test]
    public void CheckIssuer_RootParent_AllowsPurposeButNotRoot()
    {
        var root = Ca(CaPurpose.Root);

        Assert.That(CaProfileCatalog.CheckIssuer(root, CaProfileCatalog.Get(CaPurpose.TlsSsl)).IsSuccess, Is.True);
        Assert.That(CaProfileCatalog.CheckIssuer(root, CaProfileCatalog.Get(CaPurpose.Root)).Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public void EvaluateForParent_NoParent_AllowsAllSevenInOrder()
    {
        var options = CaProfileCatalog.EvaluateForParent(null);

        Assert.That(options.Select(o => o.Profile.Purpose), Is.EqualTo(CaProfileCatalog.GetAll().Select(p => p.Purpose)));
        Assert.That(options.All(o => o.IsAllowed && o.DisabledReason == null), Is.True);
    }

    [Test]
    public void EvaluateForParent_RootParent_DisablesOnlyRoot()
    {
        var options = CaProfileCatalog.EvaluateForParent(Ca(CaPurpose.Root));

        Assert.That(options.Where(o => !o.IsAllowed).Select(o => o.Profile.Purpose), Is.EqualTo(new[] { CaPurpose.Root }));
        Assert.That(options.Single(o => o.Profile.Purpose == CaPurpose.Root).DisabledReason, Does.Contain("Root"));
    }

    [Test]
    public void EvaluateForParent_TlsParent_AllowsOnlyTlsAndCustom()
    {
        var options = CaProfileCatalog.EvaluateForParent(Ca(CaPurpose.TlsSsl));

        Assert.That(options.Where(o => o.IsAllowed).Select(o => o.Profile.Purpose), Is.EqualTo(new[] { CaPurpose.TlsSsl, CaPurpose.Custom }));
        Assert.That(options.Where(o => !o.IsAllowed).All(o => !string.IsNullOrEmpty(o.DisabledReason)), Is.True);
        Assert.That(options.Single(o => o.Profile.Purpose == CaPurpose.Smime).DisabledReason, Does.Contain("not permitted by the parent"));
    }

    [Test]
    public void EvaluateForParent_IoTParent_AllowsServerAndClientPurposesAndCustom()
    {
        var options = CaProfileCatalog.EvaluateForParent(Ca(CaPurpose.IoT));

        Assert.That(
            options.Where(o => o.IsAllowed).Select(o => o.Profile.Purpose),
            Is.EqualTo(new[] { CaPurpose.TlsSsl, CaPurpose.IoT, CaPurpose.Dot1xRadius, CaPurpose.Custom }));
    }

    [Test]
    public void EvaluateForParent_PurposeCaWithItsOwnParent_DisablesEverything()
    {
        var options = CaProfileCatalog.EvaluateForParent(Ca(CaPurpose.TlsSsl, "root", "web-ca"));

        Assert.That(options.Any(o => o.IsAllowed), Is.False);
        Assert.That(options.All(o => o.DisabledReason.Contains("end-entity")), Is.True);
    }

    [Test]
    public void EvaluateForParent_LegacyParent_AllowsEverythingExceptRoot()
    {
        var options = CaProfileCatalog.EvaluateForParent(Ca(null));

        Assert.That(options.Where(o => !o.IsAllowed).Select(o => o.Profile.Purpose), Is.EqualTo(new[] { CaPurpose.Root }));
    }

    [Test]
    public void EvaluateForParent_RestrictedCustomParent_LimitsToItsUsages()
    {
        var parent = Ca(CaPurpose.Custom);
        parent.CustomExtendedKeyUsages = [ServerAuth];

        var options = CaProfileCatalog.EvaluateForParent(parent);

        Assert.That(options.Where(o => o.IsAllowed).Select(o => o.Profile.Purpose), Is.EqualTo(new[] { CaPurpose.TlsSsl, CaPurpose.Custom }));
    }

    private static readonly CertificateKeyUsage[] SignAndEncipher =
        [CertificateKeyUsage.DigitalSignature, CertificateKeyUsage.KeyEncipherment];

    [TestCase(CaPurpose.TlsSsl, true)]
    [TestCase(CaPurpose.UserLogin, true)]
    [TestCase(CaPurpose.Smime, true)]
    [TestCase(CaPurpose.IoT, true)]
    [TestCase(CaPurpose.Dot1xRadius, true)]
    [TestCase(CaPurpose.Root, false)]
    [TestCase(CaPurpose.Custom, false)]
    public void LocksLeafUsages_IsTrueOnlyForNamedPurposesThatIssueLeaves(CaPurpose purpose, bool locks)
    {
        Assert.That(CaProfileCatalog.Get(purpose).LocksLeafUsages, Is.EqualTo(locks));
    }

    [Test]
    public void LocksLeafUsages_IsFalseForCustomWithChosenUsagesAndForNoPurpose()
    {
        Assert.That(CaProfileCatalog.CreateCustom([], [ServerAuth]).LocksLeafUsages, Is.False);
        Assert.That(CaProfileCatalog.Get(null).LocksLeafUsages, Is.False);
    }

    private static readonly CaPurpose[] LeafPurposes =
        [CaPurpose.TlsSsl, CaPurpose.UserLogin, CaPurpose.Smime, CaPurpose.IoT, CaPurpose.Dot1xRadius];

    [TestCaseSource(nameof(LeafPurposes))]
    public void GetLeafKeyUsages_Rsa_IsTheProfileFullSet(CaPurpose purpose)
    {
        var profile = CaProfileCatalog.Get(purpose);

        Assert.That(profile.GetLeafKeyUsages(Algorithm.RSA), Is.EquivalentTo(profile.LeafKeyUsages));
    }

    [TestCase(CaPurpose.TlsSsl)]
    [TestCase(CaPurpose.UserLogin)]
    [TestCase(CaPurpose.IoT)]
    [TestCase(CaPurpose.Dot1xRadius)]
    public void GetLeafKeyUsages_Ec_SigningProfilesAllowDigitalSignatureOnly(CaPurpose purpose)
    {
        Assert.That(
            CaProfileCatalog.Get(purpose).GetLeafKeyUsages(Algorithm.EllipticCurve),
            Is.EquivalentTo(new[] { CertificateKeyUsage.DigitalSignature }));
    }

    [Test]
    public void GetLeafKeyUsages_Ec_SmimeUsesKeyAgreementInsteadOfKeyEncipherment()
    {
        Assert.That(
            CaProfileCatalog.Get(CaPurpose.Smime).GetLeafKeyUsages(Algorithm.EllipticCurve),
            Is.EquivalentTo(new[]
            {
                CertificateKeyUsage.DigitalSignature,
                CertificateKeyUsage.NonRepudiation,
                CertificateKeyUsage.KeyAgreement
            }));
    }

    [TestCaseSource(nameof(LeafPurposes))]
    public void CheckLeaf_EcWithTheProfileEcUsages_Succeeds(CaPurpose purpose)
    {
        var profile = CaProfileCatalog.Get(purpose);

        var result = CaProfileCatalog.CheckLeaf(
            profile, Algorithm.EllipticCurve, profile.GetLeafKeyUsages(Algorithm.EllipticCurve), profile.LeafExtendedKeyUsages);

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [TestCase(CaPurpose.TlsSsl, CertificateKeyUsage.KeyEncipherment, "Key Encipherment")]
    [TestCase(CaPurpose.TlsSsl, CertificateKeyUsage.DataEncipherment, "Data Encipherment")]
    [TestCase(CaPurpose.Smime, CertificateKeyUsage.KeyEncipherment, "Key Encipherment")]
    [TestCase(CaPurpose.Smime, CertificateKeyUsage.DataEncipherment, "Data Encipherment")]
    [TestCase(CaPurpose.Custom, CertificateKeyUsage.KeyEncipherment, "Key Encipherment")]
    [TestCase(CaPurpose.Custom, CertificateKeyUsage.DataEncipherment, "Data Encipherment")]
    public void CheckLeaf_EcWithEncipherment_IsInvalidForEveryIssuer(CaPurpose purpose, CertificateKeyUsage usage, string described)
    {
        var profile = CaProfileCatalog.Get(purpose);

        var result = CaProfileCatalog.CheckLeaf(
            profile, Algorithm.EllipticCurve, [CertificateKeyUsage.DigitalSignature, usage], [ServerAuth]);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain(described).And.Contain("Elliptic Curve"));
    }

    [Test]
    public void CheckLeaf_RsaWithKeyEncipherment_StillSucceedsForATlsCa()
    {
        var result = CaProfileCatalog.CheckLeaf(
            CaProfileCatalog.Get(CaPurpose.TlsSsl), Algorithm.RSA,
            [CertificateKeyUsage.DigitalSignature, CertificateKeyUsage.KeyEncipherment], [ServerAuth]);

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [Test]
    public void CheckKeyUsagesForAlgorithm_EcWithKeyAgreement_Succeeds()
    {
        var result = CaProfileCatalog.CheckKeyUsagesForAlgorithm(
            Algorithm.EllipticCurve, [CertificateKeyUsage.DigitalSignature, CertificateKeyUsage.KeyAgreement]);

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [Test]
    public void CheckKeyUsagesForAlgorithm_EcNamesBothOffendingUsages()
    {
        var result = CaProfileCatalog.CheckKeyUsagesForAlgorithm(
            Algorithm.EllipticCurve, [CertificateKeyUsage.KeyEncipherment, CertificateKeyUsage.DataEncipherment]);

        Assert.That(result.Error, Does.Contain("Key Encipherment").And.Contain("Data Encipherment"));
    }

    [Test]
    public void CheckLeaf_RootIssuer_IsInvalid()
    {
        var result = CaProfileCatalog.CheckLeaf(CaProfileCatalog.Get(CaPurpose.Root), Algorithm.RSA, SignAndEncipher, [ServerAuth]);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("Root").And.Contain("intermediate"));
    }

    [TestCase(CaPurpose.TlsSsl)]
    [TestCase(CaPurpose.UserLogin)]
    [TestCase(CaPurpose.Smime)]
    [TestCase(CaPurpose.IoT)]
    [TestCase(CaPurpose.Dot1xRadius)]
    public void CheckLeaf_TheProfilesOwnLeafUsages_Succeed(CaPurpose purpose)
    {
        var profile = CaProfileCatalog.Get(purpose);

        var result = CaProfileCatalog.CheckLeaf(profile, Algorithm.RSA, profile.LeafKeyUsages, profile.LeafExtendedKeyUsages);

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [Test]
    public void CheckLeaf_ASubsetOfTheAllowedUsages_Succeeds()
    {
        var result = CaProfileCatalog.CheckLeaf(CaProfileCatalog.Get(CaPurpose.IoT), Algorithm.RSA,
            [CertificateKeyUsage.DigitalSignature],
            [ClientAuth]);

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [Test]
    public void CheckLeaf_ExtraExtendedKeyUsage_IsInvalidAndNamesIt()
    {
        var result = CaProfileCatalog.CheckLeaf(CaProfileCatalog.Get(CaPurpose.TlsSsl), Algorithm.RSA, SignAndEncipher, [ServerAuth, ClientAuth]);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("Client Authentication").And.Contain("TLS/SSL"));
    }

    [Test]
    public void CheckLeaf_ExtraKeyUsage_IsInvalidAndNamesIt()
    {
        var result = CaProfileCatalog.CheckLeaf(CaProfileCatalog.Get(CaPurpose.TlsSsl), Algorithm.RSA,
            [CertificateKeyUsage.DigitalSignature, CertificateKeyUsage.NonRepudiation],
            [ServerAuth]);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("Non Repudiation"));
    }

    [Test]
    public void CheckLeaf_EncipherOnlyOnATlsCa_IsInvalid()
    {
        var result = CaProfileCatalog.CheckLeaf(CaProfileCatalog.Get(CaPurpose.TlsSsl), Algorithm.RSA, [CertificateKeyUsage.EncipherOnly], [ServerAuth]);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("Encipher Only"));
    }

    [Test]
    public void CheckLeaf_NoExtendedKeyUsages_IsInvalid()
    {
        var result = CaProfileCatalog.CheckLeaf(CaProfileCatalog.Get(CaPurpose.TlsSsl), Algorithm.RSA, SignAndEncipher, []);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("extended key usage"));
    }

    [Test]
    public void CheckLeaf_NoKeyUsages_IsInvalid()
    {
        var result = CaProfileCatalog.CheckLeaf(CaProfileCatalog.Get(CaPurpose.TlsSsl), Algorithm.RSA, [], [ServerAuth]);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("key usage"));
    }

    [Test]
    public void CheckLeaf_UnrestrictedCustomIssuer_AllowsAnything()
    {
        var result = CaProfileCatalog.CheckLeaf(CaProfileCatalog.Get(CaPurpose.Custom), Algorithm.RSA,
            [CertificateKeyUsage.EncipherOnly, CertificateKeyUsage.NonRepudiation],
            [ServerAuth, ClientAuth, Email]);

        Assert.That(result.IsSuccess, Is.True, result.Error);
    }

    [Test]
    public void CheckLeaf_CustomIssuerWithChosenExtendedKeyUsages_LimitsThemButNotKeyUsages()
    {
        var issuer = CaProfileCatalog.CreateCustom([], [ServerAuth]);

        Assert.That(CaProfileCatalog.CheckLeaf(issuer, Algorithm.RSA, [CertificateKeyUsage.EncipherOnly], [ServerAuth]).IsSuccess, Is.True);

        var outside = CaProfileCatalog.CheckLeaf(issuer, Algorithm.RSA, [CertificateKeyUsage.DigitalSignature], [ServerAuth, ClientAuth]);
        Assert.That(outside.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(outside.Error, Does.Contain("Client Authentication"));
    }

    [Test]
    public void CustomUnderARestrictedParent_IsOfferedButStillNeedsExplicitUsagesToBeSaved()
    {
        var parent = Ca(CaPurpose.TlsSsl);

        Assert.That(CaProfileCatalog.EvaluateForParent(parent).Single(o => o.Profile.Purpose == CaPurpose.Custom).IsAllowed, Is.True);

        var noUsages = CaProfileCatalog.CheckIssuer(parent, CaProfileCatalog.CreateCustom([], []));
        Assert.That(noUsages.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(noUsages.Error, Does.Contain("explicit usages"));
    }

    [Test]
    public void Known_ListsTheProfileUsagesAndTheRestWithNames()
    {
        var known = KeyPurposeOids.Known.ToDictionary(k => k.Key, k => k.Value);

        Assert.That(known.Keys, Is.SupersetOf(new[] { ServerAuth, ClientAuth, Email, SmartCard, KeyPurposeOids.CodeSigning }));
        Assert.That(known.Count, Is.EqualTo(11));
        Assert.That(known.Values.All(name => !string.IsNullOrWhiteSpace(name)), Is.True);
        Assert.That(known.Keys.Distinct().Count(), Is.EqualTo(known.Count));
    }

    [TestCase(KeyPurposeOids.CodeSigning, "Code Signing")]
    [TestCase("1.3.6.1.5.5.7.3.8", "Time Stamping")]
    [TestCase(KeyPurposeOids.SmartCardLogin, "Smart Card Login")]
    public void GetName_CoversEveryKnownUsage(string oid, string name)
    {
        Assert.That(KeyPurposeOids.GetName(oid), Is.EqualTo(name));
    }

    [TestCase(CertificateKeyUsage.DigitalSignature, "Digital Signature")]
    [TestCase(CertificateKeyUsage.KeyCertSign, "Certificate Key Sign")]
    [TestCase(CertificateKeyUsage.CrlSign, "CRL Sign")]
    [TestCase(CertificateKeyUsage.EncipherOnly, "Encipher Only")]
    public void DescribeKeyUsage_SpacesTheName(CertificateKeyUsage usage, string expected)
    {
        Assert.That(CaProfileCatalog.DescribeKeyUsage(usage), Is.EqualTo(expected));
    }

    [Test]
    public void GetName_KnownAndUnknownOids()
    {
        Assert.That(KeyPurposeOids.GetName(SmartCard), Is.EqualTo("Smart Card Login"));
        Assert.That(KeyPurposeOids.GetName("1.2.3"), Is.EqualTo("1.2.3"));
    }
}
