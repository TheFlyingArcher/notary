
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

using Notary;
using Notary.Contract;
using Notary.Interface.Service;
using Notary.Web.Shared;
using Notary.Web.ViewModels;

namespace Notary.Web.Pages;

public partial class CertificateAuthorities : ComponentBase
{
    private List<CertificateAuthority> CaList { get; set; } = new();
    private DownloadCertificateViewModel DownloadModel { get; set; } = new();
    private bool _isLoading;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        CaList = await CaService.GetAllAsync();
        _isLoading = false;
        await base.OnInitializedAsync();
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
    public IJSRuntime JS { get; set; }

    [Inject]
    public IDialogService Dialog { get; set; }

    [Inject]
    public ICertificateAuthorityService CaService { get; set; }

    [Inject]
    public ICertificateService CertificateService { get; set; }

    [Inject]
    public NavigationManager NavigationManager { get; set; }

}