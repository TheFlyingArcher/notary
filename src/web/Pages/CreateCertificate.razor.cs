using System;
using Microsoft.AspNetCore.Components;
using MudBlazor;

using Notary;
using C = Notary.Contract;
using Notary.Interface.Service;
using Notary.Web.ViewModels;
using System.Text.RegularExpressions;

namespace Notary.Web.Pages;

public partial class CreateCertificate : ComponentBase
{
    private IEnumerable<C.CertificateAuthority> caList = new List<C.CertificateAuthority>();
    private MudForm form;
    private bool _isLoading = true;
    private int _selectedCertKeyUsage = 0;
    private string _selectedExKeyUsage = string.Empty;
    private string _sanText = string.Empty;
    private SanKind _sanKind = SanKind.Dns;
    private bool _sanDialogVisible = false;
    private DialogOptions _sanDialogOptions = new DialogOptions()
    {
        BackdropClick = false,
        CloseButton = true,
        FullWidth = true,
        MaxWidth = MaxWidth.Medium
    };

    private bool success;
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
        };

        _isLoading = true;
        await Task.Run(async () =>
        {
            caList = await CertificateAuthorityService.GetAllAsync();
            caList = caList.Where(ca => ca.IsIssuer);
        });

        _isLoading = false;
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

    private void OnSanDialogOpenClick() => _sanDialogVisible = true;

    private void OnSanDialogCloseClick()
    {
        var san = new C.SubjectAlternativeName
        {
            Kind = _sanKind,
            Name = _sanText
        };
        ViewModel.SubjectAlternativeNames.Add(san);

        _sanText = string.Empty;
        _sanKind = SanKind.Dns;
        _sanDialogVisible = false;
    }


    private void OnSanDialogDeleteClick(C.SubjectAlternativeName san)
    {
        ViewModel.SubjectAlternativeNames.Remove(san);
    }

    private async Task OnSubmitAsync()
    {
        await form.Validate();

        if (!success)
            return;

        DateTime notBefore = DateTime.UtcNow;
        DateTime notAfter = DateTime.UtcNow.AddMonths(ViewModel.ExpiryLength);
        TimeSpan ts = notAfter - notBefore;
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
        await Task.Run(async () => await CertificateService.IssueCertificateAsync(request));
        _isLoading = false;
        NavigationManager.NavigateTo("certificates");
    }

    private IEnumerable<string> ValidateExpiration(int months)
    {
        if (months == 0)
        {
            yield return "Please enter a value";
        }

        if (months > 0 && months < 12)
        {
            yield return "Please enter greater than 12 months";
        }

        if (ViewModel.SelectedCa != null)
        {
            DateTime notBefore = ViewModel.SelectedCa.NotBefore;
            DateTime notAfterCa = ViewModel.SelectedCa.NotAfter;
            DateTime notAfterCert = DateTime.UtcNow.AddMonths(months);

            if (notAfterCa < notAfterCert)
            {
                yield return "Certificate validity cannot be longer than the CA validity.";
            }
        }
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
    public NavigationManager NavigationManager { get; set; }

    public CreateCertificateViewModel ViewModel { get; set; } = new();
}
