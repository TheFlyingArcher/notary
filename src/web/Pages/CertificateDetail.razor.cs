using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

using MudBlazor;

using Notary.Contract;
using Notary.Interface.Service;
using Notary.Web.Shared;
using Notary.Web.ViewModels;

namespace Notary.Web.Pages;

[Authorize(Roles = "NotaryAdmin,NotaryWriter,NotaryUser")]
public partial class CertificateDetail : ComponentBase
{
    protected CertificateViewModel Model { get; } = new();
    protected bool IsLoading { get; set; } = false;
    protected bool NotFound { get; set; } = false;
    protected string Slug { get; set; } = string.Empty;


    protected override async Task OnInitializedAsync()
    {
        var uri = NavManager.ToAbsoluteUri(NavManager.Uri);

        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("slug", out var querySlug))
        {
            Slug = querySlug;
        }
        else
        {
            NotFound = true;
            return;
        }

        IsLoading = true;
        Certificate? c = null;
        AsymmetricKey? key = null;
        RevocatedCertificate? rc = null;

        c = await CertificateService.GetAsync(Slug);
        if (c == null)
        {
            NotFound = true;
            IsLoading = false;
            return;
        }

        key = await KeyService.GetAsync(c.KeySlug);
        if (key == null)
        {
            NotFound = true;
            IsLoading = false;
            return;
        }

        if (c.RevocationDate.HasValue)
        {
            var rcList = await RevokeSvc.GetRevocatedCertificates();
            rc = rcList.Find(cc => cc.Slug == c.Thumbprint);
        }

        DateTime utcNow = DateTime.UtcNow;
        Model.EllipticCurve = key.KeyCurve;
        Model.Expired = utcNow > c.NotAfter;
        Model.Expiring = utcNow > c.NotAfter.AddDays(-30) && utcNow <= c.NotAfter;
        Model.Issuer = c.Issuer;
        Model.KeyAlgorithm = key.KeyAlgorithm;
        Model.Name = c.Name;
        Model.NotAfter = c.NotAfter;
        Model.NotBefore = c.NotBefore;
        Model.RevocationDate = c.RevocationDate;
        Model.RsaKeyLength = key.KeyLength;
        Model.SerialNumber = c.SerialNumber;
        Model.SignatureAlgorithm = c.SignatureAlgorithm;
        Model.Subject = c.Subject;
        Model.SubjectAlternativeNames = c.SubjectAlternativeNames;
        Model.Thumbprint = c.Thumbprint;

        if (rc != null)
        {
            Model.RevocationReason = rc.Reason.RevocationFriendlyName();
        }

        await PopulateIssuerTree(c.Slug);
        IsLoading = false;
    }

    protected async Task OnCertificateDownloadClick()
    {
        var parameters = new DialogParameters<DownloadCertificateDialog>
        {
            { d=> d.Slug, Slug }
        };
        var dialog = await DlgService.ShowAsync<DownloadCertificateDialog>("Download Certificate", parameters);
        var result = await dialog.Result;
    }

    protected async Task OnRevokeCertificateClick()
    {
        var parameters = new DialogParameters<RevokeCertificateDialog>
        {
            { d=> d.Slug, Slug }
        };
        var dialog = await DlgService.ShowAsync<RevokeCertificateDialog>("Revoke Certificate", parameters);
        var result = await dialog.Result;
        if (result == null)
        {
            // I don't see this can be null
            throw new ArgumentNullException(nameof(result));
        }

        if (!result.Canceled)
        {
            NavManager.NavigateTo("/certificates");
        }
    }

    private async Task PopulateIssuerTree(string slug, List<TreeItemData<CertificateIssuerTreeItem>> children = null)
    {
        var certificate = await CertificateService.GetAsync(slug);
        if (certificate == null)
        {
            throw new ArgumentNullException(nameof(certificate));
        }

        var caItem = new CertificateIssuerTreeItem
        {
            Name = certificate.Name,
            Slug = certificate.Slug
        };

        var rootItem = new TreeItemData<CertificateIssuerTreeItem>()
        {
            Value = caItem
        };

        if (!string.IsNullOrEmpty(certificate.IssuingSlug))
        {
            rootItem.Children = new List<TreeItemData<CertificateIssuerTreeItem>>();
            await PopulateIssuerTree(certificate.IssuingSlug, rootItem.Children);
        }
        if (children != null)
            children.Add(rootItem);
        else
            Model.Issuers.Add(rootItem);
    }

    [Inject]
    public ICertificateRevokeService RevokeSvc { get; set; }

    [Inject]
    public IAsymmetricKeyService KeyService { get; set; }

    [Inject]
    public ICertificateAuthorityService CaService { get; set; }

    [Inject]
    public ICertificateService CertificateService { get; set; }

    [Inject]
    public IDialogService DlgService { get; set; }

    [Inject]
    public NavigationManager NavManager { get; set; }
}