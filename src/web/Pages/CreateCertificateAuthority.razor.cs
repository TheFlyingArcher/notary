using C = Notary.Contract;
using Notary.Interface.Service;
using Notary.Profiles;
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
        private bool _submitAttempted;
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

                RefreshPurpose();
                RefreshCustomUsages();
                RefreshValidity();
                await InvokeAsync(StateHasChanged);
            };

            IsLoading = true;
            await Task.Run(async () =>
            {
                ViewModel.CertificateAuthorities = await CertificateAuthorityService.GetAllAsync();
            });
            IsLoading = false;

            if (IsIntermediateMode)
            {
                _parentCa = ViewModel.CertificateAuthorities.FirstOrDefault(ca => ca.Slug == Parent);
                if (_parentCa == null)
                {
                    _parentError = "The parent certificate authority was not found.";
                }
                else if (!CaProfileCatalog.CheckCanIssueCas(_parentCa).IsSuccess)
                {
                    _parentError = $"{_parentCa.Name} issues end-entity certificates only, so it cannot be the parent of another CA.";
                }
                else
                {
                    ViewModel.ParentCaSlug = _parentCa.Slug;
                }
            }
            else if (IsRootMode)
            {
                // A Root CA is self-signed and always has the Root purpose, so neither is offered as a choice
                ViewModel.ParentCaSlug = "nop";
                ViewModel.SelectedPurpose = CaPurpose.Root;
                OnPurposeChanged();
            }

            RefreshValidity();
        }

        private async Task OnSubmitAsync()
        {
            _submitAttempted = true;
            await _mudForm.Validate();
            if (!_formValid || CustomUsagesMissing)
                return;

            bool custom = ViewModel.SelectedPurpose == CaPurpose.Custom;
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
                Purpose = ViewModel.SelectedPurpose,
                // The two fixed usages are always sent for Custom. Sending them is what marks this as a deliberate
                // choice rather than a CA created without a purpose, which would get the old broad usages.
                CustomKeyUsages = custom ? [.. AlwaysIncludedKeyUsages.Concat(ViewModel.SelectedCustomKeyUsages.Where(OptionalKeyUsages.Contains)).Distinct()] : null,
                CustomExtendedKeyUsages = custom ? [.. ViewModel.SelectedCustomExtendedKeyUsages.Distinct()] : null,
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

        // Every CA certificate carries these; a CA cannot sign certificates or publish revocations without them
        private static readonly CertificateKeyUsage[] AlwaysIncludedKeyUsages =
            [CertificateKeyUsage.KeyCertSign, CertificateKeyUsage.CrlSign];

        private static readonly CertificateKeyUsage[] AllOptionalKeyUsages =
        [
            CertificateKeyUsage.DigitalSignature,
            CertificateKeyUsage.NonRepudiation,
            CertificateKeyUsage.KeyEncipherment,
            CertificateKeyUsage.DataEncipherment,
            CertificateKeyUsage.KeyAgreement,
            CertificateKeyUsage.EncipherOnly,
            CertificateKeyUsage.DecipherOnly
        ];

        // An EC key cannot encipher (RFC 5480), so those usages are not offered for it, whether the key is chosen here or inherited from the parent
        private IEnumerable<CertificateKeyUsage> OptionalKeyUsages =>
            AllOptionalKeyUsages.Where(u => CaProfileCatalog.CheckKeyUsagesForAlgorithm(ViewModel.KeyType, [u]).IsSuccess);

        // The chosen parent's profile; a parent that restricts extended key usages limits what a Custom child may carry
        private CaProfile ParentProfile =>
            ViewModel.SelectedCa == null ? null : CaProfileCatalog.Resolve(ViewModel.SelectedCa);

        private bool ParentRestrictsUsages => ParentProfile?.RestrictsExtendedKeyUsages == true;

        private string ParentPurposeName => ParentProfile?.DisplayName ?? string.Empty;

        private bool IsExtendedKeyUsagePermitted(string oid) =>
            !ParentRestrictsUsages || ParentProfile.CaExtendedKeyUsages.Contains(oid);

        private bool CustomUsagesMissing =>
            _submitAttempted && ViewModel.SelectedPurpose == CaPurpose.Custom && ParentRestrictsUsages
            && ViewModel.SelectedCustomExtendedKeyUsages.Count == 0;

        private string CustomUsagesMissingText =>
            $"A Custom CA under a {ParentPurposeName} CA must list its extended key usages, within the parent's.";

        private string CustomExtendedKeyUsageHelp => ParentRestrictsUsages
            ? $"Required. Limited to what the {ParentPurposeName} CA allows."
            : "Optional. Leave empty for no extended key usage restriction.";

        // Drop chosen extended key usages that a newly chosen parent does not allow
        private void RefreshCustomUsages()
        {
            ViewModel.SelectedCustomExtendedKeyUsages = [.. ViewModel.SelectedCustomExtendedKeyUsages.Where(IsExtendedKeyUsagePermitted)];
        }

        // An intermediate CA is never a Root, so that option is not offered
        private IReadOnlyList<CaPurposeOption> PurposeOptions =>
            [.. CaProfileCatalog.EvaluateForParent(ViewModel.SelectedCa).Where(o => !IsIntermediateMode || o.Profile.Purpose != CaPurpose.Root)];

        private CaProfile SelectedProfile =>
            ViewModel.SelectedPurpose.HasValue ? CaProfileCatalog.Get(ViewModel.SelectedPurpose) : null;

        private static string ParentLabel(C.CertificateAuthority ca) =>
            $"{ca.Name} ({(ca.Purpose.HasValue ? CaProfileCatalog.Get(ca.Purpose).DisplayName : "Legacy")})";

        // Root never issues certificates; every other purpose is expected to, so it defaults to an issuer
        private void OnPurposeChanged()
        {
            ViewModel.IsIssuer = ViewModel.SelectedPurpose != CaPurpose.Root;
        }

        // Clear a purpose that the newly chosen parent cannot issue
        private void RefreshPurpose()
        {
            if (!ViewModel.SelectedPurpose.HasValue)
                return;

            var option = PurposeOptions.FirstOrDefault(o => o.Profile.Purpose == ViewModel.SelectedPurpose);
            if (option == null || !option.IsAllowed)
                ViewModel.SelectedPurpose = null;
        }

        private string PurposeSummary
        {
            get
            {
                var profile = SelectedProfile;
                if (profile == null)
                    return string.Empty;

                switch (profile.Purpose)
                {
                    case CaPurpose.Root:
                        return "Signs intermediate CAs only. It does not issue certificates directly and carries no usage restriction.";
                    case CaPurpose.Custom:
                        return "Discouraged. The usages you choose below define this CA, and a mistake can weaken it or break trust chains. " +
                            "Prefer one of the purposes above unless none of them fits.";
                    default:
                        string usages = string.Join(", ", profile.CaExtendedKeyUsages.Select(KeyPurposeOids.GetName));
                        string reach = ViewModel.SelectedCa == null
                            ? "As a standalone CA it can also issue CAs of the same purpose."
                            : "Under a parent it issues end-entity certificates only, not other CAs.";
                        return $"Certificates from this CA are limited to: {usages}. {reach}";
                }
            }
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

        private IEnumerable<string> ValidateCommonName(string commonName)
        {
            var problem = C.DistinguishedName.CheckCommonName(commonName);
            if (problem != null)
                yield return problem;
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

        [SupplyParameterFromQuery(Name = "root")]
        public bool Root { get; set; }

        [SupplyParameterFromQuery(Name = "parent")]
        public string Parent { get; set; }

        private C.CertificateAuthority _parentCa;
        private string _parentError;

        // Root wins when both are given, since a root has no parent
        private bool IsRootMode => Root;

        private bool IsIntermediateMode => !Root && !string.IsNullOrEmpty(Parent);

        private string PageTitle => IsRootMode ? "Create Root CA" : IsIntermediateMode ? "Create Intermediate CA" : "Create Certificate Authority";

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