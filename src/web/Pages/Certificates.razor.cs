using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Notary.Contract;
using Notary.Interface.Service;
using Notary.Web.Shared;

namespace Notary.Web.Pages;

[Authorize(Roles = "NotaryAdmin,NotaryWriter,NotaryUser")]
public partial class Certificates : ComponentBase
{
    private List<Certificate> certificates = new();
    private bool IsLoading { get; set; }

    [Inject] public IDialogService Dialog { get; set; }

    [Inject] public ICertificateService CertificateService { get; set; }

    [Inject] public NavigationManager NavigationManager { get; set; }

    protected override async Task OnInitializedAsync()
    {
        IsLoading = true;
        await Task.Run(async () =>
        {
            certificates = await CertificateService.GetAllAsync();
            certificates = certificates.Where(c => !c.IsCaCertificate).ToList();
        });

        IsLoading = false;
    }

    private async Task OnCertificateDownloadClick(string slug)
    {
        var parameters = new DialogParameters<DownloadCertificateDialog>
        {
            { d => d.Slug, slug }
        };
        var dialog = await Dialog.ShowAsync<DownloadCertificateDialog>("Download Certificate", parameters);
        var result = await dialog.Result;
    }
}