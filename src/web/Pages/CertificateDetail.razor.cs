using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
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
        Revocation? rc = null;

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
            rc = await RevokeSvc.GetRevocationAsync(c.Slug);
        }

        DateTime utcNow = DateTime.UtcNow;
        Model.EllipticCurve = key.KeyCurve;
        var status = CertificateStatusCalculator.Get(c.NotAfter, c.RevocationDate, utcNow);
        Model.Expired = status == CertificateStatus.Expired;
        Model.Expiring = status == CertificateStatus.Expiring;
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

        Model.IsCaCertificate = c.IsCaCertificate;

        if (rc != null)
        {
            Model.RevocationReason = rc.Reason.RevocationFriendlyName();
            Model.IsOnHold = rc.Reason == RevocationReason.CertificateHold;
        }

        if (c.IsCaCertificate)
        {
            await PopulateCaInfoAsync(c.Slug);
            await PopulateIssuedCertificatesAsync(c.Slug, utcNow);
            await PopulateCrlAsync(c.Slug);
        }

        // A root has no issuers above it, so there is no tree to show
        if (!Model.IsRootCa)
        {
            await PopulateIssuerTree(c.Slug);
        }

        IsLoading = false;
    }

    private async Task PopulateCaInfoAsync(string certificateSlug)
    {
        // The detail page is keyed by certificate slug, while purpose and parent live on the CA record
        var authorities = await CaService.GetAllAsync();
        var ca = authorities.FirstOrDefault(a => a.CertificateSlug == certificateSlug);
        if (ca == null)
        {
            Model.CaKind = "Unknown";
            return;
        }

        Model.CaKind = CaDetailInfo.KindName(ca);
        Model.IsRootCa = CaDetailInfo.IsRoot(ca);
    }

    private async Task PopulateIssuedCertificatesAsync(string certificateSlug, DateTime utcNow)
    {
        // Keyed by the CA's certificate slug, the same value the certificates page passes
        var issued = await CertificateService.GetCertificatesByCaAsync(certificateSlug);
        foreach (var cert in IssuedCertificateFilter.ForCa(issued, Model.IsRootCa))
        {
            Model.IssuedCertificates.Add(new IssuedCertificateRow(
                cert.Slug,
                cert.Name,
                cert.Subject?.CommonName ?? string.Empty,
                cert.NotAfter,
                CertificateStatusCalculator.Get(cert.NotAfter, cert.RevocationDate, utcNow),
                cert.IsCaCertificate,
                cert.RevocationDate.HasValue));
        }
    }

    protected async Task OnIssuedCertificateDownloadClick(IssuedCertificateRow row)
    {
        var parameters = new DialogParameters<DownloadCertificateDialog>
        {
            { d=> d.Slug, row.Slug },
            { d=> d.IsCaCertificate, row.IsCaCertificate }
        };
        var dialog = await DlgService.ShowAsync<DownloadCertificateDialog>("Download Certificate", parameters);
        await dialog.Result;
    }

    protected static Color StatusColor(CertificateStatus status) => status switch
    {
        CertificateStatus.Expired => Color.Error,
        CertificateStatus.Expiring => Color.Warning,
        CertificateStatus.Revoked => Color.Info,
        _ => Color.Success
    };

    private async Task PopulateCrlAsync(string caSlug)
    {
        var baseUrl = CrlBaseUrlProvider.GetCrlBaseUrl();
        if (!string.IsNullOrEmpty(baseUrl))
        {
            Model.CrlUrl = $"{baseUrl}/{Uri.EscapeDataString(caSlug)}";
        }

        // Issues and caches the CRL on first use, exactly as a client request to the CRL endpoint would
        var crl = await RevokeSvc.GetCrlAsync(caSlug);
        if (crl.IsSuccess)
        {
            Model.CrlNumber = crl.Value.Number;
            Model.CrlNextUpdate = crl.Value.NextUpdate;
        }
        else
        {
            Model.CrlUnavailableReason = crl.Error;
        }
    }

    protected async Task OnCertificateDownloadClick()
    {
        var parameters = new DialogParameters<DownloadCertificateDialog>
        {
            { d=> d.Slug, Slug },
            { d=> d.IsCaCertificate, Model.IsCaCertificate }
        };
        var dialog = await DlgService.ShowAsync<DownloadCertificateDialog>("Download Certificate", parameters);
        var result = await dialog.Result;
    }

    protected async Task OnRevokeCertificateClick()
    {
        var parameters = new DialogParameters<RevokeCertificateDialog>
        {
            { d=> d.Slug, Slug },
            { d=> d.IsCaCertificate, Model.IsCaCertificate }
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
            NavManager.NavigateTo(ListUrl);
        }
    }

    protected async Task OnReinstateCertificateClick()
    {
        var confirmed = await DlgService.ShowMessageBoxAsync(
            "Reinstate Certificate",
            Model.IsCaCertificate
                ? "This lifts the temporary revocation. The certificate authority, and the certificates it issued, will be trusted again once clients refresh the CRL."
                : "This lifts the temporary revocation. The certificate will be trusted again once clients refresh the CRL.",
            yesText: "Reinstate",
            cancelText: "Cancel");

        if (confirmed != true)
            return;

        var authState = await AuthProvider.GetAuthenticationStateAsync();
        var userName = authState.User?.Identity?.Name;

        var result = await RevokeSvc.ReinstateCertificateAsync(Slug, userName);
        if (!result.IsSuccess)
        {
            Snackbar.Add(result.Error, Severity.Error);
            return;
        }

        Snackbar.Add("Certificate reinstated", Severity.Success);
        NavManager.NavigateTo(NavManager.Uri, forceLoad: true);
    }

    // CA certificates are not shown on the certificates list, so return to the CA list instead
    protected string ListUrl => Model.IsCaCertificate ? "/ca" : "/certificates";

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
            var issuerChildren = new List<TreeItemData<CertificateIssuerTreeItem>>();
            await PopulateIssuerTree(certificate.IssuingSlug, issuerChildren);
            rootItem.Children = issuerChildren;
        }
        if (children != null)
            children.Add(rootItem);
        else
            Model.Issuers.Add(rootItem);
    }

    [Inject]
    public AuthenticationStateProvider AuthProvider { get; set; }

    [Inject]
    public ICrlBaseUrlProvider CrlBaseUrlProvider { get; set; }

    [Inject]
    public ISnackbar Snackbar { get; set; }

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