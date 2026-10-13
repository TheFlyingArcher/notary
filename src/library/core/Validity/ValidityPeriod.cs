using System;
using System.Globalization;

namespace Notary.Validity;

/// <summary>
/// The unit a <see cref="ValidityPeriod"/> is measured in
/// </summary>
public enum ValidityUnit
{
    Months,
    Days
}

/// <summary>
/// A certificate validity length, e.g. 48 months or 90 days
/// </summary>
/// <param name="Amount">The number of units</param>
/// <param name="Unit">The unit of measure</param>
public sealed record ValidityPeriod(int Amount, ValidityUnit Unit)
{
    /// <summary>
    /// Get the end of the period when it starts at <paramref name="start"/>
    /// </summary>
    public DateTime AddTo(DateTime start)
    {
        return Unit == ValidityUnit.Months ? start.AddMonths(Amount) : start.AddDays(Amount);
    }

    /// <summary>
    /// Get the display text, e.g. "48 months (4 years)" or "90 days". Years are shown only for whole years.
    /// </summary>
    public string Format()
    {
        if (Unit == ValidityUnit.Days)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{Amount} {(Amount == 1 ? "day" : "days")}");
        }

        string months = string.Create(CultureInfo.InvariantCulture, $"{Amount} {(Amount == 1 ? "month" : "months")}");
        if (Amount >= 12 && Amount % 12 == 0)
        {
            int years = Amount / 12;
            return string.Create(CultureInfo.InvariantCulture, $"{months} ({years} {(years == 1 ? "year" : "years")})");
        }

        return months;
    }
}
