namespace Notary.Profiles;

/// <summary>
/// A CA purpose as offered when creating a CA under a given parent
/// </summary>
/// <param name="Profile">The purpose's profile</param>
/// <param name="IsAllowed">False if a CA of this purpose cannot be issued by the parent</param>
/// <param name="DisabledReason">Why the option is not allowed; null when allowed</param>
public sealed record CaPurposeOption(CaProfile Profile, bool IsAllowed, string DisabledReason);
