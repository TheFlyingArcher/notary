using Markdig;

namespace Notary.Web.Services;

/// <summary>
/// Turns release notes into HTML. Raw HTML in the notes is not passed through.
/// </summary>
public static class ChangelogMarkdown
{
    private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .UseAutoLinks()
        .Build();

    public static string ToHtml(string markdown)
    {
        return Markdown.ToHtml(markdown ?? string.Empty, _pipeline);
    }
}
