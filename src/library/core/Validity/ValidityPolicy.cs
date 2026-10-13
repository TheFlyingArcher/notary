using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Notary.Validity;

/// <summary>
/// The validity periods offered for each kind of certificate, and the rule that nothing may outlive its issuer
/// </summary>
public static class ValidityPolicy
{
    private static readonly ValidityPeriod[] _rootPeriods =
    [
        new(120, ValidityUnit.Months),
        new(180, ValidityUnit.Months),
        new(240, ValidityUnit.Months)
    ];

    private static readonly ValidityPeriod[] _intermediatePeriods =
    [
        new(36, ValidityUnit.Months),
        new(60, ValidityUnit.Months),
        new(120, ValidityUnit.Months)
    ];

    private static readonly ValidityPeriod[] _leafPeriods =
    [
        new(30, ValidityUnit.Days),
        new(90, ValidityUnit.Days),
        new(6, ValidityUnit.Months),
        new(12, ValidityUnit.Months),
        new(13, ValidityUnit.Months),
        new(24, ValidityUnit.Months)
    ];

    /// <summary>
    /// Get the offered periods for a role, shortest first
    /// </summary>
    public static IReadOnlyList<ValidityPeriod> GetPeriods(CertificateRole role)
    {
        return role switch
        {
            CertificateRole.RootCa => _rootPeriods,
            CertificateRole.IntermediateCa => _intermediatePeriods,
            CertificateRole.Leaf => _leafPeriods,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }

    /// <summary>
    /// Get the recommended period for a role
    /// </summary>
    public static ValidityPeriod GetRecommended(CertificateRole role)
    {
        return role switch
        {
            CertificateRole.RootCa => new ValidityPeriod(240, ValidityUnit.Months),
            CertificateRole.IntermediateCa => new ValidityPeriod(60, ValidityUnit.Months),
            CertificateRole.Leaf => new ValidityPeriod(12, ValidityUnit.Months),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }

    /// <summary>
    /// Check that a certificate's validity is contained within its issuer's validity
    /// </summary>
    /// <param name="notBefore">The start of the new certificate's validity</param>
    /// <param name="notAfter">The end of the new certificate's validity</param>
    /// <param name="issuerNotBefore">The start of the issuing CA's validity</param>
    /// <param name="issuerNotAfter">The end of the issuing CA's validity</param>
    /// <returns>Success, or an Invalid result describing the violation</returns>
    public static Result CheckWithinIssuer(DateTime notBefore, DateTime notAfter, DateTime issuerNotBefore, DateTime issuerNotAfter)
    {
        if (notAfter <= notBefore)
        {
            return Result.Fail(ResultStatus.Invalid, "The validity period must end after it begins.");
        }

        if (notAfter > issuerNotAfter)
        {
            return Result.Fail(
                ResultStatus.Invalid,
                string.Create(CultureInfo.InvariantCulture, $"The validity period cannot extend beyond the issuing CA's expiry ({issuerNotAfter:yyyy-MM-dd})."));
        }

        if (notBefore < issuerNotBefore)
        {
            return Result.Fail(
                ResultStatus.Invalid,
                string.Create(CultureInfo.InvariantCulture, $"The validity period cannot begin before the issuing CA's start ({issuerNotBefore:yyyy-MM-dd})."));
        }

        return Result.Ok();
    }

    /// <summary>
    /// Get the dropdown options for a role. An option is disallowed when a certificate issued at
    /// <paramref name="now"/> with that period would expire after <paramref name="issuerNotAfter"/>.
    /// </summary>
    /// <param name="role">The kind of certificate being created</param>
    /// <param name="now">The issue time</param>
    /// <param name="issuerNotAfter">The issuing CA expiry, or null for a self-signed root</param>
    /// <remarks>
    /// The recommended option is always an allowed one: the role's recommended period when it fits, otherwise
    /// the longest allowed period that is shorter than it. If nothing fits, no option is recommended.
    /// </remarks>
    public static IReadOnlyList<ValidityOption> Evaluate(CertificateRole role, DateTime now, DateTime? issuerNotAfter)
    {
        DateTime recommendedEnd = GetRecommended(role).AddTo(now);

        var candidates = GetPeriods(role)
            .Select(period => (Period: period, End: period.AddTo(now)))
            .Select(c => (c.Period, c.End, Allowed: issuerNotAfter is null || c.End <= issuerNotAfter.Value))
            .ToList();

        ValidityPeriod? suggested = candidates
            .Where(c => c.Allowed && c.End <= recommendedEnd)
            .OrderByDescending(c => c.End)
            .Select(c => (ValidityPeriod?)c.Period)
            .FirstOrDefault();

        return candidates
            .Select(c => new ValidityOption(
                c.Period,
                c.Period.Format(),
                c.Period == suggested,
                c.Allowed,
                c.Allowed
                    ? null
                    : string.Create(CultureInfo.InvariantCulture, $"Exceeds issuer expiry ({issuerNotAfter.Value:yyyy-MM-dd})")))
            .ToList();
    }
}
