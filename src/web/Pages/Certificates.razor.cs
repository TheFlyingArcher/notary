using Notary;
using Notary.Contract;
using Notary.Interface.Service;
using Notary.Web.ViewModels;
using Notary.Web.Shared;

using MudBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authorization;

namespace Notary.Web.Pages;

[Authorize(Roles = "NotaryAdmin,NotaryWriter,NotaryUser")]
public partial class Certificates : ComponentBase
{
    private bool IsLoading { get; set; } = false;
    private List<Certificate> certificates = new List<Certificate>();

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
            { d=> d.Slug, slug }
        };
        var dialog = await Dialog.ShowAsync<DownloadCertificateDialog>("Download Certificate", parameters);
        var result = await dialog.Result;
    }

    [Inject]
    public IDialogService Dialog { get; set; }

    [Inject]
    public ICertificateService CertificateService { get; set; }

    [Inject]
    public NavigationManager NavigationManager { get; set; }
}