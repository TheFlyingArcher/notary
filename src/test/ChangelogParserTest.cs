using Notary.Changelog;

namespace Notary.Test;

public class ChangelogParserTest
{
    private const string Header = "<!--\nformat notes\n-->\n\n# Changelog\n\n";

    [TestCase("")]
    [TestCase("  \n\n")]
    public void Parse_EmptyOrWhitespace_ReturnsNoEntries(string markdown)
    {
        Assert.That(ChangelogParser.Parse(markdown), Is.Empty);
    }

    [Test]
    public void Parse_Null_ReturnsNoEntries()
    {
        Assert.That(ChangelogParser.Parse(null), Is.Empty);
    }

    [Test]
    public void Parse_HeaderOnly_ReturnsNoEntries()
    {
        Assert.That(ChangelogParser.Parse(Header), Is.Empty);
    }

    [Test]
    public void Parse_ReadsVersionDateAndNotes()
    {
        var entries = ChangelogParser.Parse(Header + "## v1.2.19 - 2026-10-29\n\nFirst note.\n\n- Second note\n");

        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].Version, Is.EqualTo("v1.2.19"));
        Assert.That(entries[0].Date, Is.EqualTo(new DateOnly(2026, 10, 29)));
        Assert.That(entries[0].Notes, Is.EqualTo("First note.\n\n- Second note"));
    }

    [Test]
    public void Parse_KeepsFileOrder()
    {
        var entries = ChangelogParser.Parse(Header + "## v1.2.19 - 2026-10-29\n\nB\n\n## v1.2.14 - 2026-10-26\n\nA\n");

        Assert.That(entries.Select(e => e.Version), Is.EqualTo(new[] { "v1.2.19", "v1.2.14" }));
        Assert.That(entries.Select(e => e.Notes), Is.EqualTo(new[] { "B", "A" }));
    }

    [Test]
    public void Parse_LevelFourHeadingsStayInTheNotes()
    {
        var entries = ChangelogParser.Parse(Header + "## v1.2.19 - 2026-10-29\n\n#### Deployment\n- Fixed\n\n## v1.2.14 - 2026-10-26\n\nOld\n");

        Assert.That(entries, Has.Count.EqualTo(2));
        Assert.That(entries[0].Notes, Is.EqualTo("#### Deployment\n- Fixed"));
    }

    [Test]
    public void Parse_EntryWithNoNotes_HasEmptyNotes()
    {
        var entries = ChangelogParser.Parse("## v1.0.0 - 2024-01-01\n## v0.9.0 - 2023-12-01\n");

        Assert.That(entries, Has.Count.EqualTo(2));
        Assert.That(entries[0].Notes, Is.Empty);
    }

    [TestCase("## v1.2.19 - 2026-13-45")]
    [TestCase("## 1.2.19 - 2026-10-29")]
    [TestCase("## v1.2 - 2026-10-29")]
    [TestCase("## v1.2.19")]
    [TestCase("### v1.2.19 - 2026-10-29")]
    [TestCase("    ## v1.2.19 - 2026-10-29")]
    public void Parse_MalformedHeading_IsNotAnEntry(string heading)
    {
        Assert.That(ChangelogParser.Parse(heading + "\n\nnotes\n"), Is.Empty);
    }

    [Test]
    public void Parse_MalformedHeading_StaysInThePreviousNotes()
    {
        var entries = ChangelogParser.Parse("## v1.0.0 - 2024-01-01\n\nHello\n\n## v1.0.1 - 2024-99-99\n\nWorld\n");

        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].Notes, Does.Contain("World"));
    }

    [Test]
    public void Parse_WindowsLineEndings_AreHandled()
    {
        var entries = ChangelogParser.Parse("## v1.0.0 - 2024-01-01\r\n\r\nHello\r\n\r\n## v0.9.0 - 2023-12-01\r\n\r\nOld\r\n");

        Assert.That(entries, Has.Count.EqualTo(2));
        Assert.That(entries[0].Notes, Is.EqualTo("Hello"));
    }

    [Test]
    public void Parse_PublishedChangelog_HasEveryReleaseNewestFirst()
    {
        var entries = ChangelogParser.Parse(File.ReadAllText(FindChangelog()));

        Assert.That(entries.Select(e => e.Version), Is.EqualTo(new[]
        {
            "v1.2.19", "v1.2.14", "v1.1.29", "v1.1.19", "v1.0.18", "v1.0.15"
        }));
        Assert.That(entries.Select(e => e.Date), Is.Ordered.Descending);
        Assert.That(entries.Select(e => e.Notes), Has.None.Empty);
    }

    private static string FindChangelog()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "web", "CHANGELOG.md");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("src/web/CHANGELOG.md was not found above the test directory.");
    }
}
