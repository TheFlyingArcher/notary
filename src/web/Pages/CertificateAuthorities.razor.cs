
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

using Notary;
using Notary.Contract;
using Notary.Interface.Service;
using Notary.Profiles;
using Notary.Web.Shared;
using Notary.Web.ViewModels;

namespace Notary.Web.Pages;

public partial class CertificateAuthorities : ComponentBase
{
    private List<CertificateAuthority> CaList { get; set; } = new();
    private List<CaHierarchyRow> CaRows { get; set; } = new();
    private DownloadCertificateViewModel DownloadModel { get; set; } = new();
    private bool _isLoading;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        CaList = await CaService.GetAllAsync();
        CaRows = CaHierarchy.Flatten(CaList);
        _isLoading = false;
        await base.OnInitializedAsync();
    }

    private async Task OnCertificateDownloadClick(string slug)
    {
        var parameters = new DialogParameters<DownloadCertificateDialog>
        {
            { d=> d.Slug, slug },
            { d=> d.IsCaCertificate, true }
        };
        var dialog = await Dialog.ShowAsync<DownloadCertificateDialog>("Download Certificate", parameters);
        var result = await dialog.Result;
    }

    private static string PurposeName(CertificateAuthority ca) =>
        ca.Purpose.HasValue ? CaProfileCatalog.Get(ca.Purpose).DisplayName : "Legacy";

    // Roots show no icon; every other CA shows its purpose, and a CA created before purposes existed counts as Custom
    private static string PurposeIcon(CaHierarchyRow row) =>
        row.Authority.Purpose == CaPurpose.Root || (row.Authority.Purpose == null && row.Depth == 0)
            ? null
            : CaPurposeIcons.ForSubordinate(row.Authority.Purpose);

    private static bool CanAddIntermediate(CertificateAuthority ca) =>
        CaProfileCatalog.CheckCanIssueCas(ca).IsSuccess;

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