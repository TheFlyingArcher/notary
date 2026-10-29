using Notary.Changelog;

namespace Notary.Web.Services;

/// <summary>
/// Reads CHANGELOG.md, which is published beside the application, once and keeps the parsed entries
/// </summary>
public class ChangelogProvider(string filePath)
{
    private readonly Lazy<Result<IReadOnlyList<ChangelogEntry>>> _entries = new(() => Load(filePath));

    public Result<IReadOnlyList<ChangelogEntry>> GetEntries()
    {
        return _entries.Value;
    }

    private static Result<IReadOnlyList<ChangelogEntry>> Load(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return Result<IReadOnlyList<ChangelogEntry>>.Fail(ResultStatus.NotFound, "The change log file was not found.");
            }

            return Result<IReadOnlyList<ChangelogEntry>>.Ok(ChangelogParser.Parse(File.ReadAllText(filePath)));
        }
        catch (IOException)
        {
            return Result<IReadOnlyList<ChangelogEntry>>.Fail(ResultStatus.Failed, "The change log file could not be read.");
        }
        catch (UnauthorizedAccessException)
        {
            return Result<IReadOnlyList<ChangelogEntry>>.Fail(ResultStatus.Failed, "The change log file could not be read.");
        }
    }
}
