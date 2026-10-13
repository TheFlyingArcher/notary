using Microsoft.AspNetCore.Components;
using MudBlazor;

using Notary;
using C = Notary.Contract;
using Notary.Interface.Service;
using Notary.Validity;
using Notary.Web.ViewModels;
using Notary.Web.Shared;

namespace Notary.Web.Pages;

public partial class CreateSelfSignedCertificate : ComponentBase
{
    private MudForm form;
    private bool _isLoading;
    private int _selectedCertKeyUsage = 0;
    private string _selectedExKeyUsage = string.Empty;
    private bool success;
    private bool _submitAttempted;
    private string[] errors = { };

    protected override void OnInitialized()
    {
        RefreshValidity();
    }

    private async Task OnSanDialogOpenClick()
    {
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
            SubjectAlternativeNames = ViewModel.SubjectAlternativeNames,
            ParentCertificateSlug = null
        };

        if (ViewModel.KeyAlgorithm == Algorithm.RSA)
            request.KeySize = ViewModel.KeySize;
        else if (ViewModel.KeyAlgorithm == Algorithm.EllipticCurve)
            request.Curve = ViewModel.Curve;

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

    // A self-signed certificate has no issuer to cap it
    private IReadOnlyList<ValidityOption> ValidityOptions =>
        ValidityPolicy.Evaluate(CertificateRole.Leaf, DateTime.UtcNow, null);

    // Keep the current choice if it is still allowed, otherwise fall back to the recommended or longest allowed period
    private void RefreshValidity()
    {
        var allowed = ValidityOptions.Where(o => o.IsAllowed).ToList();

        if (ViewModel.SelectedPeriod != null && allowed.Any(o => o.Period == ViewModel.SelectedPeriod))
            return;

        ViewModel.SelectedPeriod = (allowed.FirstOrDefault(o => o.IsRecommended) ?? allowed.LastOrDefault())?.Period;
    }

    [Inject]
    public ICertificateService CertificateService { get; set; }

    [Inject]
    public IDialogService DialogService { get; set; }

    [Inject]
    public ISnackbar Snackbar { get; set; }

    [Inject]
    public NavigationManager NavigationManager { get; set; }

    public CreateCertificateViewModel ViewModel { get; set; } = new();
}
