using System;
using Microsoft.AspNetCore.Components;
using MudBlazor;

using Notary;
using C = Notary.Contract;
using Notary.Interface.Service;
using Notary.Validity;
using Notary.Web.ViewModels;
using System.Text.RegularExpressions;
using Notary.Web.Shared;

namespace Notary.Web.Pages;

public partial class CreateCertificate : ComponentBase
{
    private IEnumerable<C.CertificateAuthority> caList = new List<C.CertificateAuthority>();
    private MudForm form;
    private bool _isLoading = true;
    private int _selectedCertKeyUsage = 0;
    private string _selectedExKeyUsage = string.Empty;
    private bool success;
    private bool _submitAttempted;
    private string[] errors = { };

    protected override async Task OnInitializedAsync()
    {
        ViewModel.OnCertificateAuthoritySlugChanged += async (c) =>
        {
            if (c != null && c != "self")
            {
                _isLoading = true;
                await Task.Run(async () =>
                {
                    ViewModel.SelectedCa = await CertificateAuthorityService.GetAsync(c);
                });
                _isLoading = false;

                ViewModel.Subject.Country = ViewModel.SelectedCa.DistinguishedName.Country;
                ViewModel.Subject.Locale = ViewModel.SelectedCa.DistinguishedName.Locale;
                ViewModel.Subject.Organization = ViewModel.SelectedCa.DistinguishedName.Organization;
                ViewModel.Subject.OrganizationalUnit = ViewModel.SelectedCa.DistinguishedName.OrganizationalUnit;
                ViewModel.Subject.StateProvince = ViewModel.SelectedCa.DistinguishedName.StateProvince;

                ViewModel.KeyAlgorithm = ViewModel.SelectedCa.KeyAlgorithm;
                if (ViewModel.SelectedCa.KeyAlgorithm == Algorithm.RSA && ViewModel.SelectedCa.KeyLength.HasValue)
                    ViewModel.KeySize = ViewModel.SelectedCa.KeyLength.Value;
                else if (ViewModel.SelectedCa.KeyAlgorithm == Algorithm.EllipticCurve && ViewModel.SelectedCa.KeyCurve.HasValue)
                    ViewModel.Curve = ViewModel.SelectedCa.KeyCurve.Value;
                else
                    throw new InvalidCastException("Invalid values for either key size or key curve");
            }
            else
            {
                ViewModel.Reset();
            }

            RefreshValidity();
            await InvokeAsync(StateHasChanged);
        };

        _isLoading = true;
        await Task.Run(async () =>
        {
            caList = await CertificateAuthorityService.GetAllAsync();
            caList = caList.Where(ca => ca.IsIssuer);
        });

        _isLoading = false;

        RefreshValidity();
    }

    private async Task OnCaListChange(string slug)
    {
        if (slug == null)
            return;

        _isLoading = true;
        ViewModel.CertificateAuthoritySlug = slug;

        if (slug == "self")
        {
            ViewModel.SelectedCa = null;
        }
        else
        {
            await Task.Run(async () =>
            {
                ViewModel.SelectedCa = await CertificateAuthorityService.GetAsync(slug);
            });
        }
        _isLoading = false;
    }

    private async Task OnSanDialogOpenClick()
    {
        var options = new DialogOptions()
        {
            BackdropClick = false,
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Medium
        };

        var dialog = await DialogService.ShowAsync<SubjectAlternativeNameDialog>("Subject Alternative Name");
        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            if (result.Data == null)
                throw new ArgumentNullException(nameof(result.Data)); // This should not be the case

            var san = result.Data as C.SubjectAlternativeName;
            ViewModel.SubjectAlternativeNames.Add(san);
        }
    }

    private void OnSanDialogDeleteClick(C.SubjectAlternativeName san)
    {
        ViewModel.SubjectAlternativeNames.Remove(san);
    }

    private bool KeyUsageMissing => _submitAttempted && ViewModel.SelectedCertificateKeyUsage.Count == 0;

    private async Task OnSubmitAsync()
    {
        _submitAttempted = true;
        await form.Validate();

        // An empty Key Usage extension is invalid (RFC 5280), so at least one usage is required
        if (!success || KeyUsageMissing)
            return;

        DateTime notBefore = DateTime.UtcNow;
        DateTime notAfter = ViewModel.SelectedPeriod.AddTo(notBefore);
        var request = new C.CertificateRequest
        {
            CertificateKeyUsageFlags = ViewModel.SelectedCertificateKeyUsage,
            IsCaCertificate = false,
            KeyAlgorithm = ViewModel.KeyAlgorithm,
            Name = ViewModel.Name,
            NotAfter = notAfter,
            NotBefore = notBefore,
            ExtendedKeyUsages = ViewModel.SelectedExKeyUsages,
            Subject = ViewModel.Subject,
            SubjectAlternativeNames = ViewModel.SubjectAlternativeNames
        };

        if (ViewModel.KeyAlgorithm == Algorithm.RSA)
            request.KeySize = ViewModel.KeySize;
        else if (ViewModel.KeyAlgorithm == Algorithm.EllipticCurve)
            request.Curve = ViewModel.Curve;

        request.ParentCertificateSlug = ViewModel.SelectedCa != null ? ViewModel.SelectedCa.CertificateSlug : null;

        _isLoading = true;
        var result = await Task.Run(async () => await CertificateService.IssueCertificateAsync(request));
        _isLoading = false;

        if (!result.IsSuccess)
        {
            Snackbar.Add(result.Error, Severity.Error);
            return;
        }

        NavigationManager.NavigateTo("certificates");
    }

    // A self-signed certificate has no issuer to cap it; an issuing CA caps the certificate at the CA's expiry
    private IReadOnlyList<ValidityOption> ValidityOptions =>
        ValidityPolicy.Evaluate(CertificateRole.Leaf, DateTime.UtcNow, ViewModel.SelectedCa?.NotAfter);

    private bool AnyValidityAllowed => ValidityOptions.Any(o => o.IsAllowed);

    private string ValidityHelperText => AnyValidityAllowed
        ? "Choose how long the certificate is valid. It cannot outlive its issuing CA."
        : "No validity period fits within the issuing CA's remaining lifetime.";

    // Keep the current choice if it is still allowed, otherwise fall back to the recommended or longest allowed period
    private void RefreshValidity()
    {
        var allowed = ValidityOptions.Where(o => o.IsAllowed).ToList();

        if (ViewModel.SelectedPeriod != null && allowed.Any(o => o.Period == ViewModel.SelectedPeriod))
            return;

        ViewModel.SelectedPeriod = (allowed.FirstOrDefault(o => o.IsRecommended) ?? allowed.LastOrDefault())?.Period;
    }

    private IEnumerable<string> ValidateName(string name)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
        {
            yield return "Please enter a certificate name";
        }

        var regex = new Regex("[a-zA-Z0-9\\-]");
        if (!regex.IsMatch(name))
        {
            yield return "Name can only contain alphanumerics and dashes";
        }
    }

    [Inject]
    public ICertificateService CertificateService { get; set; }

    [Inject]
    public ICertificateAuthorityService CertificateAuthorityService { get; set; }

    [Inject]
    public IDialogService DialogService { get; set; }

    [Inject]
    public ISnackbar Snackbar { get; set; }

    [Inject]
    public NavigationManager NavigationManager { get; set; }

    public CreateCertificateViewModel ViewModel { get; set; } = new();
}
