using Microsoft.AspNetCore.Components;
using MudBlazor;
using Notary.Contract;

namespace Notary.Web.Shared;

public partial class SubjectAlternativeNameDialog : ComponentBase
{
    private SanKind _sanKind = SanKind.Dns;
    private string _sanText = string.Empty;

    public SubjectAlternativeNameDialog()
    {
        _sanKind = SanKind.Dns;
        _sanText = string.Empty;
    }

    [CascadingParameter] private MudDialogInstance MudDialog { get; set; }

    private void OnOK()
    {
        var san = new SubjectAlternativeName
        {
            Name = _sanText,
            Kind = _sanKind
        };

        MudDialog.Close(DialogResult.Ok(san));
    }
}