namespace Notary.Validity;

/// <summary>
/// A validity period as presented in a dropdown, evaluated against the issuer expiry
/// </summary>
/// <param name="Period">The validity period</param>
/// <param name="Label">The display text</param>
/// <param name="IsRecommended">True for the suggested choice: always an allowed option, never a disabled one</param>
/// <param name="IsAllowed">False if the period outlives the issuer</param>
/// <param name="DisabledReason">Why the option is not allowed; null when allowed</param>
public sealed record ValidityOption(
    ValidityPeriod Period,
    string Label,
    bool IsRecommended,
    bool IsAllowed,
    string DisabledReason);
