using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Notary;
using Notary.Contract;
using Notary.Download;
using Notary.Interface.Service;
using Notary.Web.ViewModels;

namespace Notary.Web.Shared;

public partial class DownloadCertificateDialog : ComponentBase
{
    private MudForm _form;
    private bool _isValid = false;
    private string[] _errors = { };

    public DownloadCertificateDialog()
    {

    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }

    private void OnCancel()
    {
        MudDialog.Cancel();
    }

    private async Task OnOk()
    {
        await _form.Validate();

        if (_isValid)
        {
            Certificate? cert = null;
            byte[]? certBinary = null;
            var password = SelectedOption.RequiresPassword ? ViewModel.Password : null;
            await Task.Run(async () =>
            {
                cert = await CertificateService.GetAsync(Slug);
                certBinary = await CertificateService.RequestCertificateAsync(Slug, ViewModel.Format, password);
            });
            if (cert == null)
                throw new ArgumentNullException(nameof(cert));

            var fileName = CertificateDownloadCatalog.BuildFileName(ViewModel.Format, ViewModel.FileName, cert.Name);
            if (certBinary != null)
            {
                using (var stream = new MemoryStream(certBinary))
                {
                    using var streamRef = new DotNetStreamReference(stream, false);
                    await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
                }
            }
            MudDialog.Close();
        }
    }

    private void OnFormatChanged(CertificateFormat format)
    {
        ViewModel.Format = format;
        // A password typed for one format must not linger once its fields are hidden
        ViewModel.Password = null;
        ViewModel.ConfirmPassword = null;
    }

    private IEnumerable<string> PasswordStrength(string pw)
    {
        if (string.IsNullOrWhiteSpace(pw) && !SelectedOption.RequiresPassword)
            yield break;

        if (string.IsNullOrWhiteSpace(pw) && SelectedOption.RequiresPassword)
        {
            yield return $"A password is required for the {SelectedOption.Label}.";
            yield break;
        }

        if (pw.Length < 8)
            yield return "Password must be at least of length 8";
        if (!Regex.IsMatch(pw, @"[A-Z]"))
            yield return "Password must contain at least one capital letter";
        if (!Regex.IsMatch(pw, @"[a-z]"))
            yield return "Password must contain at least one lowercase letter";
        if (!Regex.IsMatch(pw, @"[0-9]"))
            yield return "Password must contain at least one digit";
    }

    private string? PasswordMatch(string pw)
    {
        if (ViewModel.Password != pw && SelectedOption.RequiresPassword)
            return "Passwords must match";

        return null;
    }

    [Inject]
    public IJSRuntime JS { get; set; }

    [Inject]
    public IDialogService Dialog { get; set; }

    [Inject]
    public ICertificateService CertificateService { get; set; }

    // Start on the first option so the blurb always has a format to describe
    private DownloadCertificateViewModel ViewModel { get; set; } = new() { Format = CertificateDownloadCatalog.All[0].Format };

    private CertificateDownloadOption SelectedOption => CertificateDownloadCatalog.For(ViewModel.Format);

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    [Parameter] public string Slug { get; set; }

    // A CA's private key never leaves Notary, so the dialog does not offer the key bundle for one
    [Parameter] public bool IsCaCertificate { get; set; }

    private IReadOnlyList<CertificateDownloadOption> Options => CertificateDownloadCatalog.ForCertificate(IsCaCertificate);
}