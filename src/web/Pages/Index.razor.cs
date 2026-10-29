using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Notary.Changelog;
using Notary.Web.Services;

namespace Notary.Web.Pages;

[Authorize]
public partial class Index : ComponentBase
{
    protected IReadOnlyList<ChangelogEntry> Entries { get; private set; } = [];

    protected string? LoadError { get; private set; }

    protected override void OnInitialized()
    {
        var result = ChangelogProvider.GetEntries();

        if (result.IsSuccess)
        {
            Entries = result.Value;
        }
        else
        {
            LoadError = result.Error;
        }
    }

    protected static MarkupString Render(string notes)
    {
        return new MarkupString(ChangelogMarkdown.ToHtml(notes));
    }

    [Inject]
    public ChangelogProvider ChangelogProvider { get; set; }
}
