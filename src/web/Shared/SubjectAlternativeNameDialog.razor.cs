using Microsoft.AspNetCore.Components;
using MudBlazor;
using Notary.Contract;
using Notary.Web.ViewModels;

namespace Notary.Web.Shared;

public partial class SubjectAlternativeNameDialog : ComponentBase
{
    private string _sanText = string.Empty;
    private SanKind _sanKind = SanKind.Dns;

    public SubjectAlternativeNameDialog()
    {
        _sanKind = SanKind.Dns;
        _sanText = string.Empty;
    }

    private void OnOK()
    {
        var san = new SubjectAlternativeName
        {
            Name = _sanText,
            Kind = _sanKind
        };

        MudDialog.Close(DialogResult.Ok(san));
    }

    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; }
}