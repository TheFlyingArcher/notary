using Notary;
using Notary.Contract;
using Notary.Interface.Service;
using Notary.Web.ViewModels;
using Notary.Web.Shared;

using MudBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Authorization;

namespace Notary.Web.Pages;

[Authorize(Roles = "NotaryAdmin,NotaryWriter,NotaryUser")]
public partial class Certificates : ComponentBase
{
    private const string SelfSignedScope = "self";
    private const string ScopeStorageKey = "certificates.selectedCa";

    private bool IsLoading { get; set; } = true;
    private List<Certificate> certificates = new List<Certificate>();
    private List<CertificateAuthority> caList = new List<CertificateAuthority>();
    private string selectedScope = SelfSignedScope;

    private string EmptyMessage
    {
        get
        {
            var ca = SelectedCa;
            return ca == null
                ? "There are no self-signed certificates."
                : $"{ca.Name} has not issued any certificates yet.";
        }
    }

    // Create Certificate always issues from the selected CA
    private string CreateCertificateHref => SelectedCa == null
        ? null
        : $"/certificates/create?ca={Uri.EscapeDataString(SelectedCa.Slug)}";

    private CertificateAuthority SelectedCa => caList.FirstOrDefault(ca => ca.Slug == selectedScope);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Browser storage needs JS interop, which is only available once the page has rendered
        if (!firstRender)
            return;

        caList = await CertificateAuthorityService.GetLeafIssuingAuthoritiesAsync();
        selectedScope = await ResolveInitialScopeAsync();
        await LoadCertificatesAsync();

        IsLoading = false;
        StateHasChanged();
    }

    private async Task<string> ResolveInitialScopeAsync()
    {
        string stored = null;
        try
        {
            var result = await LocalStorage.GetAsync<string>(ScopeStorageKey);
            if (result.Success)
                stored = result.Value;
        }
        catch (Exception)
        {
            // Unavailable or tampered storage: fall back to the default
        }

        if (stored == SelfSignedScope || caList.Any(ca => ca.Slug == stored))
            return stored;

        return caList.Count > 0 ? caList[0].Slug : SelfSignedScope;
    }

    private async Task LoadCertificatesAsync()
    {
        var ca = SelectedCa;
        var loaded = ca == null
            ? await CertificateService.GetSelfSignedCertificatesAsync()
            : await CertificateService.GetCertificatesByCaAsync(ca.CertificateSlug);

        certificates = loaded.Where(c => !c.IsCaCertificate).ToList();
    }

    private async Task OnScopeChangedAsync(string scope)
    {
        selectedScope = scope;
        IsLoading = true;
        await LoadCertificatesAsync();
        IsLoading = false;

        try
        {
            await LocalStorage.SetAsync(ScopeStorageKey, scope);
        }
        catch (Exception)
        {
            // The choice just isn't remembered
        }
    }

    private async Task OnCertificateDownloadClick(string slug)
    {
        var parameters = new DialogParameters<DownloadCertificateDialog>
        {
            { d=> d.Slug, slug },
            { d=> d.IsCaCertificate, false }
        };
        var dialog = await Dialog.ShowAsync<DownloadCertificateDialog>("Download Certificate", parameters);
        var result = await dialog.Result;
    }

    [Inject]
    public IDialogService Dialog { get; set; }

    [Inject]
    public ICertificateService CertificateService { get; set; }

    [Inject]
    public ICertificateAuthorityService CertificateAuthorityService { get; set; }

    [Inject]
    public ProtectedLocalStorage LocalStorage { get; set; }

    [Inject]
    public NavigationManager NavigationManager { get; set; }
}