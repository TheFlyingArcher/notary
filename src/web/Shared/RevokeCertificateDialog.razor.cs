using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Notary;
using Notary.Contract;
using Notary.Interface.Service;
using Notary.Web.ViewModels;
using System.Text.RegularExpressions;

namespace Notary.Web.Shared;

public partial class RevokeCertificateDialog : ComponentBase
{
    protected MudForm _form;
    private bool _isValid = false;
    private string[] _errors = { };
    public RevokeCertificateDialog()
    {

    }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthProvider.GetAuthenticationStateAsync();
        if (authState.User != null && authState.User.Identity != null)
        {
            Model.UserRevoking = authState.User.Identity.Name;
        }
    }

    protected void OnCancel()
    {
        MudDialog.Cancel();
    }

    protected async Task OnOk()
    {
        await RevocationService.RevokeCertificateAsync(Slug, Model.RevocationReason, Model.UserRevoking);
        MudDialog.Close();
    }

    [CascadingParameter] private MudDialogInstance MudDialog { get; set; }

    [Parameter] public string Slug { get; set; }

    protected RevokeCertificateDialogViewModel Model { get; } = new();

    [Inject]
    public ICertificateRevokeService RevocationService { get; set; }

    [Inject]
    public IDialogService Dialog { get; set; }

    [Inject]
    public AuthenticationStateProvider AuthProvider { get; set; }
}