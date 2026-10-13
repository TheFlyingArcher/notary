using Notary.Validity;

namespace Notary.Test;

public class ValidityPolicyTest
{
    private static readonly DateTime _now = new(2026, 1, 31, 12, 0, 0, DateTimeKind.Utc);

    [TestCase(CertificateRole.RootCa, new[] { 120, 180, 240 })]
    [TestCase(CertificateRole.IntermediateCa, new[] { 36, 60, 120 })]
    public void GetPeriods_CaRoles_ReturnsMonthLists(CertificateRole role, int[] months)
    {
        var periods = ValidityPolicy.GetPeriods(role);

        Assert.That(periods.Select(p => p.Amount), Is.EqualTo(months));
        Assert.That(periods.All(p => p.Unit == ValidityUnit.Months), Is.True);
    }

    [Test]
    public void GetPeriods_Leaf_IncludesDayAndMonthOptions()
    {
        var periods = ValidityPolicy.GetPeriods(CertificateRole.Leaf);

        Assert.That(periods, Is.EqualTo(new[]
        {
            new ValidityPeriod(30, ValidityUnit.Days),
            new ValidityPeriod(90, ValidityUnit.Days),
            new ValidityPeriod(6, ValidityUnit.Months),
            new ValidityPeriod(12, ValidityUnit.Months),
            new ValidityPeriod(13, ValidityUnit.Months),
            new ValidityPeriod(24, ValidityUnit.Months)
        }));
    }

    [TestCase(CertificateRole.RootCa)]
    [TestCase(CertificateRole.IntermediateCa)]
    [TestCase(CertificateRole.Leaf)]
    public void Evaluate_AnyRole_HasExactlyOneRecommendedFromTheList(CertificateRole role)
    {
        var options = ValidityPolicy.Evaluate(role, _now, null);

        Assert.That(options.Count(o => o.IsRecommended), Is.EqualTo(1));
        Assert.That(options.Single(o => o.IsRecommended).Period, Is.EqualTo(ValidityPolicy.GetRecommended(role)));
        Assert.That(ValidityPolicy.GetPeriods(role), Does.Contain(ValidityPolicy.GetRecommended(role)));
    }

    [TestCase(48, ValidityUnit.Months, "48 months (4 years)")]
    [TestCase(12, ValidityUnit.Months, "12 months (1 year)")]
    [TestCase(6, ValidityUnit.Months, "6 months")]
    [TestCase(13, ValidityUnit.Months, "13 months")]
    [TestCase(1, ValidityUnit.Months, "1 month")]
    [TestCase(90, ValidityUnit.Days, "90 days")]
    [TestCase(1, ValidityUnit.Days, "1 day")]
    public void Format_ReturnsExpectedText(int amount, ValidityUnit unit, string expected)
    {
        Assert.That(new ValidityPeriod(amount, unit).Format(), Is.EqualTo(expected));
    }

    [Test]
    public void AddTo_Months_ClampsToEndOfShorterMonth()
    {
        Assert.That(new ValidityPeriod(1, ValidityUnit.Months).AddTo(_now), Is.EqualTo(new DateTime(2026, 2, 28, 12, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void AddTo_Days_AddsCalendarDays()
    {
        Assert.That(new ValidityPeriod(90, ValidityUnit.Days).AddTo(_now), Is.EqualTo(_now.AddDays(90)));
    }

    [Test]
    public void Evaluate_NoIssuer_AllowsEverything()
    {
        var options = ValidityPolicy.Evaluate(CertificateRole.RootCa, _now, null);

        Assert.That(options.All(o => o.IsAllowed && o.DisabledReason == null), Is.True);
    }

    [Test]
    public void Evaluate_IssuerExpiresInFourYears_DisablesLongerIntermediateOptions()
    {
        DateTime issuerNotAfter = _now.AddMonths(48);

        var leaf = ValidityPolicy.Evaluate(CertificateRole.Leaf, _now, issuerNotAfter);
        Assert.That(leaf.All(o => o.IsAllowed), Is.True);

        var intermediate = ValidityPolicy.Evaluate(CertificateRole.IntermediateCa, _now, issuerNotAfter);
        Assert.That(intermediate.Where(o => o.IsAllowed).Select(o => o.Period.Amount), Is.EqualTo(new[] { 36 }));
        Assert.That(intermediate.Single(o => o.Period.Amount == 60).DisabledReason, Does.StartWith("Exceeds issuer expiry"));
    }

    [Test]
    public void Evaluate_PeriodEndingExactlyAtIssuerExpiry_IsAllowed()
    {
        var options = ValidityPolicy.Evaluate(CertificateRole.IntermediateCa, _now, _now.AddMonths(60));

        Assert.That(options.Single(o => o.Period.Amount == 60).IsAllowed, Is.True);
        Assert.That(options.Single(o => o.Period.Amount == 120).IsAllowed, Is.False);
    }

    [Test]
    public void CheckWithinIssuer_ChildWithinIssuer_Succeeds()
    {
        var result = ValidityPolicy.CheckWithinIssuer(_now, _now.AddMonths(12), _now.AddDays(-1), _now.AddMonths(48));

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void CheckWithinIssuer_ExactBoundaries_Succeeds()
    {
        var result = ValidityPolicy.CheckWithinIssuer(_now, _now.AddMonths(48), _now, _now.AddMonths(48));

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void CheckWithinIssuer_ChildOutlivesIssuer_ReturnsInvalid()
    {
        var result = ValidityPolicy.CheckWithinIssuer(_now, _now.AddMonths(49), _now, _now.AddMonths(48));

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Error, Does.Contain("issuing CA's expiry"));
    }

    [Test]
    public void CheckWithinIssuer_ChildStartsBeforeIssuer_ReturnsInvalid()
    {
        var result = ValidityPolicy.CheckWithinIssuer(_now.AddDays(-2), _now.AddMonths(12), _now.AddDays(-1), _now.AddMonths(48));

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public void CheckWithinIssuer_EndsBeforeItBegins_ReturnsInvalid()
    {
        var result = ValidityPolicy.CheckWithinIssuer(_now, _now, _now.AddDays(-1), _now.AddMonths(48));

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
    }

    [Test]
    public void Evaluate_RecommendedDoesNotFit_RecommendsLongestAllowedShorterPeriod()
    {
        // Issuer valid for 12 months: the 12 month leaf would end exactly then, so allow only up to 12 months minus a day
        var options = ValidityPolicy.Evaluate(CertificateRole.Leaf, _now, _now.AddMonths(12).AddDays(-1));

        Assert.That(options.Single(o => o.Period.Amount == 12 && o.Period.Unit == ValidityUnit.Months).IsAllowed, Is.False);
        Assert.That(options.Single(o => o.IsRecommended).Period, Is.EqualTo(new ValidityPeriod(6, ValidityUnit.Months)));
    }

    [Test]
    public void Evaluate_RecommendedFits_StillRecommendsIt()
    {
        var options = ValidityPolicy.Evaluate(CertificateRole.Leaf, _now, _now.AddMonths(60));

        Assert.That(options.Single(o => o.IsRecommended).Period, Is.EqualTo(new ValidityPeriod(12, ValidityUnit.Months)));
    }

    [Test]
    public void Evaluate_NothingFits_RecommendsNothing()
    {
        var options = ValidityPolicy.Evaluate(CertificateRole.Leaf, _now, _now.AddDays(5));

        Assert.That(options.Any(o => o.IsAllowed), Is.False);
        Assert.That(options.Any(o => o.IsRecommended), Is.False);
    }

    [Test]
    public void Evaluate_RecommendedIsNeverADisabledOption()
    {
        foreach (var role in Enum.GetValues<CertificateRole>())
        {
            for (int days = 1; days < 365 * 25; days += 37)
            {
                var options = ValidityPolicy.Evaluate(role, _now, _now.AddDays(days));

                Assert.That(options.Where(o => o.IsRecommended).All(o => o.IsAllowed), Is.True, $"{role} with issuer {days} days out");
                Assert.That(options.Count(o => o.IsRecommended), Is.LessThanOrEqualTo(1));
            }
        }
    }

    [Test]
    public void Evaluate_IssuerAlmostExpired_DisablesAllButShortestLeaf()
    {
        var options = ValidityPolicy.Evaluate(CertificateRole.Leaf, _now, _now.AddDays(45));

        Assert.That(options.Where(o => o.IsAllowed).Select(o => o.Period), Is.EqualTo(new[] { new ValidityPeriod(30, ValidityUnit.Days) }));
    }
}
