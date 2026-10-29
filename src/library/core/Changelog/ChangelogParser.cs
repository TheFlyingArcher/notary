using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Notary.Changelog;

/// <summary>
/// Reads the entries out of CHANGELOG.md. An entry starts at a level-2 heading of the form
/// <c>## v1.2.19 - 2026-10-29</c>; everything up to the next such heading is its notes. The file's header comment and
/// title come before the first entry and are ignored.
/// </summary>
public static partial class ChangelogParser
{
    public static IReadOnlyList<ChangelogEntry> Parse(string markdown)
    {
        var entries = new List<ChangelogEntry>();
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return entries;
        }

        string version = null;
        DateOnly date = default;
        var notes = new StringBuilder();

        foreach (var line in markdown.ReplaceLineEndings("\n").Split('\n'))
        {
            if (TryParseHeading(line, out var headingVersion, out var headingDate))
            {
                AddEntry(entries, version, date, notes);
                version = headingVersion;
                date = headingDate;
                notes.Clear();
            }
            else if (version != null)
            {
                notes.Append(line).Append('\n');
            }
        }

        AddEntry(entries, version, date, notes);

        return entries;
    }

    private static void AddEntry(List<ChangelogEntry> entries, string version, DateOnly date, StringBuilder notes)
    {
        if (version != null)
        {
            entries.Add(new ChangelogEntry(version, date, notes.ToString().Trim()));
        }
    }

    private static bool TryParseHeading(string line, out string version, out DateOnly date)
    {
        version = null;
        date = default;

        var match = HeadingPattern().Match(line);

        if (!match.Success)
        {
            return false;
        }

        if (!DateOnly.TryParseExact(match.Groups["date"].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return false;
        }

        version = match.Groups["version"].Value;
        return true;
    }

    [GeneratedRegex(@"^## (?<version>v\d+\.\d+\.\d+) - (?<date>\d{4}-\d{2}-\d{2})\s*$")]
    private static partial Regex HeadingPattern();
}
