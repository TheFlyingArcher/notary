using System;
using Microsoft.AspNetCore.Components;
using MudBlazor;

using Notary;
using C = Notary.Contract;
using Notary.Interface.Service;
using Notary.Profiles;
using Notary.Security;
using Notary.Validity;
using Notary.Web.ViewModels;
using Notary.Web.Shared;

namespace Notary.Web.Pages;

public partial class CreateCertificate : ComponentBase
{
    private MudForm form;
    private bool _isLoading = true;
    private int _selectedCertKeyUsage = 0;
    private string _selectedExKeyUsage = string.Empty;
    private bool success;
    private bool _submitAttempted;
    private bool _usagesApplied;
    private string[] errors = { };

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;

        // Only a CA that can currently issue leaf certificates is eligible (not revoked, not a Root)
        var eligible = await CertificateAuthorityService.GetLeafIssuingAuthoritiesAsync();
        var ca = string.IsNullOrEmpty(CaSlug) ? null : eligible.FirstOrDefault(c => c.Slug == CaSlug);

        if (ca == null)
        {
            Snackbar.Add("Select a certificate authority that can issue certificates first.", Severity.Warning);
            NavigationManager.NavigateTo("certificates");
            return;
        }

        ApplyCa(ca);
        _isLoading = false;
    }

    private void ApplyCa(C.CertificateAuthority ca)
    {
        ViewModel.SelectedCa = ca;

        ViewModel.Subject.Country = ca.DistinguishedName.Country;
        ViewModel.Subject.Locale = ca.DistinguishedName.Locale;
        ViewModel.Subject.Organization = ca.DistinguishedName.Organization;
        ViewModel.Subject.OrganizationalUnit = ca.DistinguishedName.OrganizationalUnit;
        ViewModel.Subject.StateProvince = ca.DistinguishedName.StateProvince;

        ViewModel.KeyAlgorithm = ca.KeyAlgorithm;
        if (ca.KeyAlgorithm == Algorithm.RSA && ca.KeyLength.HasValue)
            ViewModel.KeySize = ca.KeyLength.Value;
        else if (ca.KeyAlgorithm == Algorithm.EllipticCurve && ca.KeyCurve.HasValue)
            ViewModel.Curve = ca.KeyCurve.Value;
        else
            throw new InvalidCastException("Invalid values for either key size or key curve");

        RefreshUsages();
        RefreshValidity();
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

    // An EC key cannot encipher (RFC 5480), so those usages are not offered for it
    private IEnumerable<KeyValuePair<int, string>> AvailableKeyUsages =>
        ViewModel.CertificateKeyUsages.Where(u =>
            CaProfileCatalog.CheckKeyUsagesForAlgorithm(ViewModel.KeyAlgorithm, [(CertificateKeyUsage)u.Key]).IsSuccess);

    // The issuing CA's key, which the certificate's key must be at least as strong as
    private KeySpec? IssuerKey => ViewModel.SelectedCa == null
        ? null
        : new KeySpec(ViewModel.SelectedCa.KeyAlgorithm, ViewModel.SelectedCa.KeyCurve, ViewModel.SelectedCa.KeyLength);

    private IReadOnlyList<int> AllowedRsaLengths => KeyOptions.AllowedRsaLengths(IssuerKey!);

    private IReadOnlyList<EllipticCurve> AllowedCurves => KeyOptions.AllowedCurves(IssuerKey!);

    private bool RsaOffered => AllowedRsaLengths.Count > 0;

    private bool EllipticCurveOffered => AllowedCurves.Count > 0;

    private string IssuerKeyText => IssuerKey == null ? string.Empty : KeyOptions.DescribeIssuer(IssuerKey);

    private string KeyFloorText => IssuerKey == null ? string.Empty : KeyOptions.Describe(IssuerKey);

    private static string RsaLabel(int length) => KeyOptions.Label(new KeySpec(Algorithm.RSA, null, length));

    private static string CurveLabel(EllipticCurve curve) => KeyOptions.Label(new KeySpec(Algorithm.EllipticCurve, curve, null));

    private string? RsaRelation(int length) => RelationText(new KeySpec(Algorithm.RSA, null, length));

    private string? CurveRelation(EllipticCurve curve) => RelationText(new KeySpec(Algorithm.EllipticCurve, curve, null));

    private string? RelationText(KeySpec candidate)
    {
        var relation = IssuerKey == null ? null : KeyOptions.Relate(IssuerKey, candidate);
        return relation.HasValue ? KeyOptions.RelationText(relation.Value) : null;
    }

    // Switching key type keeps the form valid: take the weakest allowed size or curve of the new type if the current one is
    // not offered, then let the usages follow the key (an EC key cannot encipher, and a purpose CA sets usages per key type)
    private void OnKeyAlgorithmChanged()
    {
        if (ViewModel.KeyAlgorithm == Algorithm.RSA && RsaOffered && !AllowedRsaLengths.Contains(ViewModel.KeySize))
            ViewModel.KeySize = AllowedRsaLengths[0];
        else if (ViewModel.KeyAlgorithm == Algorithm.EllipticCurve && EllipticCurveOffered && !AllowedCurves.Contains(ViewModel.Curve))
            ViewModel.Curve = AllowedCurves[0];

        RefreshUsages();
    }

    private bool KeyUsageMissing => _submitAttempted && ViewModel.SelectedCertificateKeyUsage.Count == 0;

    private async Task OnSubmitAsync()
    {
        _submitAttempted = true;
        // Drop usages the key cannot carry before checking that some remain
        ViewModel.SelectedCertificateKeyUsage = [.. ViewModel.SelectedCertificateKeyUsage.Where(u => AvailableKeyUsages.Any(a => a.Key == u))];
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

    // The profile of the issuing CA
    private CaProfile? IssuerProfile => ViewModel.SelectedCa == null ? null : CaProfileCatalog.Resolve(ViewModel.SelectedCa);

    // A purpose CA fixes the usages of what it issues, so they are applied instead of chosen
    private bool UsagesLocked => IssuerProfile?.LocksLeafUsages == true;

    private static string CaLabel(C.CertificateAuthority ca) =>
        $"{ca.Name} ({(ca.Purpose.HasValue ? CaProfileCatalog.Get(ca.Purpose).DisplayName : "Legacy")})";

    // Apply the issuing CA's usages, or put the editable defaults back when leaving a locked CA
    private void RefreshUsages()
    {
        var profile = IssuerProfile;

        if (profile is { LocksLeafUsages: true })
        {
            ViewModel.SelectedExKeyUsages = [.. profile.LeafExtendedKeyUsages];
            ViewModel.SelectedCertificateKeyUsage = [.. profile.GetLeafKeyUsages(ViewModel.KeyAlgorithm).Select(u => (int)u)];
            _usagesApplied = true;
        }
        else if (_usagesApplied)
        {
            ViewModel.ResetUsages();
            _usagesApplied = false;
        }
    }

    private string UsageNote
    {
        get
        {
            var profile = IssuerProfile;
            if (profile == null)
                return string.Empty;

            if (!profile.IssuesLeafCertificates)
                return $"A {profile.DisplayName} CA never issues certificates directly. Choose another CA.";

            string keyUsages = string.Join(", ", profile.GetLeafKeyUsages(ViewModel.KeyAlgorithm).Select(u =>
                ViewModel.CertificateKeyUsages.TryGetValue((int)u, out var name) ? name : u.ToString()));
            string extendedKeyUsages = string.Join(", ", profile.LeafExtendedKeyUsages.Select(oid =>
                ViewModel.ExtendedKeyUsages.TryGetValue(oid, out var name) ? name : KeyPurposeOids.GetName(oid)));

            return $"Usages are set by the {profile.DisplayName} CA. Key usages: {keyUsages}. Extended key usages: {extendedKeyUsages}.";
        }
    }

    [SupplyParameterFromQuery(Name = "ca")]
    public string CaSlug { get; set; }

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
