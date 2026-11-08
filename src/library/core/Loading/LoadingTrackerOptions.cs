using System;

namespace Notary.Loading;

/// <summary>
/// Timing of the loading overlay
/// </summary>
/// <param name="ShowDelay">How long work must run before the overlay appears</param>
/// <param name="MinimumVisible">How long the overlay stays once it appears</param>
public sealed record LoadingTrackerOptions(TimeSpan ShowDelay, TimeSpan MinimumVisible)
{
    public static LoadingTrackerOptions Default { get; } = new(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(300));
}
