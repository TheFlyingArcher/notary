using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Notary.Interface.Service;
using Notary.Web.ViewModels;

namespace Notary.Web.Shared;

public partial class RevokeCertificateDialog : ComponentBase
{
    private string[] _errors = { };
    protected MudForm _form;
    private bool _isValid;

    [CascadingParameter] private MudDialogInstance MudDialog { get; set; }

    [Parameter] public string Slug { get; set; }

    protected RevokeCertificateDialogViewModel Model { get; } = new();

    [Inject] public ICertificateRevokeService RevocationService { get; set; }

    [Inject] public IDialogService Dialog { get; set; }

    [Inject] public AuthenticationStateProvider AuthProvider { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthProvider.GetAuthenticationStateAsync();
        if (authState.User != null && authState.User.Identity != null)
            Model.UserRevoking = authState.User.Identity.Name;
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
}