using C = Notary.Contract;
using Notary.Interface.Service;
using Notary.Validity;
using Notary.Web.ViewModels;

using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authorization;
using MudBlazor;

namespace Notary.Web.Pages
{
    [Authorize(Roles = "NotaryAdmin")]
    public partial class CreateCertificateAuthority : ComponentBase
    {
        private CreateCertificateAuthorityViewModel ViewModel { get; set; } = new();
        private bool IsLoading { get; set; } = false;
        private bool _formValid = false;
        private string[] _formErrors = { };
        private MudForm _mudForm;

        protected override async Task OnInitializedAsync()
        {
            ViewModel.OnCertificateAuthoritySlugChanged += async (c) =>
            {
                if (c != null && c != "nop")
                {
                    IsLoading = true;
                    await Task.Run(async () =>
                    {
                        ViewModel.SelectedCa = await CertificateAuthorityService.GetAsync(c);
                    });
                    IsLoading = false;

                    ViewModel.Country = ViewModel.SelectedCa.DistinguishedName.Country;
                    ViewModel.Locale = ViewModel.SelectedCa.DistinguishedName.Locale;
                    ViewModel.Organization = ViewModel.SelectedCa.DistinguishedName.Organization;
                    ViewModel.OrganizationalUnit = ViewModel.SelectedCa.DistinguishedName.OrganizationalUnit;
                    ViewModel.StateProvince = ViewModel.SelectedCa.DistinguishedName.StateProvince;

                    ViewModel.KeyType = ViewModel.SelectedCa.KeyAlgorithm;
                    if (ViewModel.SelectedCa.KeyAlgorithm == Algorithm.RSA && ViewModel.SelectedCa.KeyLength.HasValue)
                        ViewModel.KeyLength = ViewModel.SelectedCa.KeyLength.Value;
                    else if (ViewModel.SelectedCa.KeyAlgorithm == Algorithm.EllipticCurve && ViewModel.SelectedCa.KeyCurve.HasValue)
                        ViewModel.Curve = ViewModel.SelectedCa.KeyCurve.Value;
                    else
                        throw new InvalidCastException("Invalid values for either key size or key curve");
                }
                else
                {
                    ViewModel.SelectedCa = null;
                    ViewModel.Country = null;
                    ViewModel.Locale = null;
                    ViewModel.Organization = null;
                    ViewModel.OrganizationalUnit = null;
                    ViewModel.StateProvince = null;

                    ViewModel.KeyType = Algorithm.RSA;
                    ViewModel.KeyLength = 2048;
                    ViewModel.Curve = EllipticCurve.P256;
                }

                RefreshValidity();
                await InvokeAsync(StateHasChanged);
            };

            IsLoading = true;
            await Task.Run(async () =>
            {
                ViewModel.CertificateAuthorities = await CertificateAuthorityService.GetAllAsync();
            });
            IsLoading = false;

            RefreshValidity();
        }

        private async Task OnSubmitAsync()
        {
            await _mudForm.Validate();
            if (!_formValid)
                return;

            DateTime now = DateTime.UtcNow;
            var ca = new C.CertificateAuthority
            {
                Active = true,
                Created = now,
                IsIssuer = ViewModel.IsIssuer,
                KeyAlgorithm = ViewModel.KeyType,
                KeyCurve = ViewModel.Curve,
                KeyLength = ViewModel.KeyLength,
                Name = ViewModel.Name,
                NotAfter = ViewModel.SelectedPeriod.AddTo(now),
                NotBefore = now,
                ParentCaSlug = ViewModel.ParentCaSlug == "nop" ? null : ViewModel.ParentCaSlug,
                Updated = now,
                DistinguishedName = new C.DistinguishedName
                {
                    CommonName = ViewModel.CommonName,
                    Country = ViewModel.SelectedCa != null ? ViewModel.SelectedCa.DistinguishedName.Country : ViewModel.Country,
                    Locale = ViewModel.SelectedCa != null ? ViewModel.SelectedCa.DistinguishedName.Locale : ViewModel.Locale,
                    Organization = ViewModel.SelectedCa != null ? ViewModel.SelectedCa.DistinguishedName.Organization :
                ViewModel.Organization,
                    OrganizationalUnit = ViewModel.SelectedCa != null ? ViewModel.SelectedCa.DistinguishedName.OrganizationalUnit :
                ViewModel.OrganizationalUnit,
                    StateProvince = ViewModel.SelectedCa != null ? ViewModel.SelectedCa.DistinguishedName.StateProvince :
                ViewModel.StateProvince
                }
            };

            IsLoading = true;
            var result = await Task.Run(async () => await CertificateAuthorityService.CreateAsync(ca, null));
            IsLoading = false;

            if (!result.IsSuccess)
            {
                Snackbar.Add(result.Error, Severity.Error);
                return;
            }

            NavigationManager.NavigateTo("ca");
        }

        private async Task OnParentCaListChange(string slug)
        {
            if (slug == null)
                return;

            ViewModel.ParentCaSlug = slug;
            ViewModel.SelectedCa = slug == string.Empty ? null : await CertificateAuthorityService.GetAsync(slug);
        }

        // A self-signed root has no issuer to cap it; a parent CA caps the child at the parent's expiry
        private CertificateRole ValidityRole => ViewModel.SelectedCa == null ? CertificateRole.RootCa : CertificateRole.IntermediateCa;

        private IReadOnlyList<ValidityOption> ValidityOptions =>
            ValidityPolicy.Evaluate(ValidityRole, DateTime.UtcNow, ViewModel.SelectedCa?.NotAfter);

        private bool AnyValidityAllowed => ValidityOptions.Any(o => o.IsAllowed);

        private string ValidityHelperText => AnyValidityAllowed
            ? "Choose how long the CA is valid. Child certificates cannot outlive it."
            : "No validity period fits within the parent CA's remaining lifetime.";

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

        // A parent CA publishes the CRL for the certificates it issues; a root CA's own certificate needs none
        private bool IsCrlHostLoopback =>
            !string.IsNullOrEmpty(ViewModel.ParentCaSlug) && ViewModel.ParentCaSlug != "nop"
            && Uri.TryCreate(CrlBaseUrl, UriKind.Absolute, out var uri) && uri.IsLoopback;

        private string CrlBaseUrl => CrlBaseUrlProvider.GetCrlBaseUrl();

        [Inject]
        public ICrlBaseUrlProvider CrlBaseUrlProvider { get; set; }

        [Inject]
        public ICertificateAuthorityService CertificateAuthorityService { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }
    }
}