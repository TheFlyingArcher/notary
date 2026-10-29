using System;

namespace Notary.Changelog;

/// <summary>
/// One release in the change log
/// </summary>
/// <param name="Version">The version with its leading v, for example v1.2.19</param>
/// <param name="Date">The release date</param>
/// <param name="Notes">The release notes, as Markdown</param>
public sealed record ChangelogEntry(string Version, DateOnly Date, string Notes);
