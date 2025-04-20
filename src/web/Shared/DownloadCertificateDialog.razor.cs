using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Notary;
using Notary.Contract;
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
            string fileName = string.Empty;
            await Task.Run(async () =>
            {
                cert = await CertificateService.GetAsync(Slug);
                certBinary = await CertificateService.RequestCertificateAsync(Slug, ViewModel.Format, ViewModel.Password);
            });
            if (cert == null)
                throw new ArgumentNullException(nameof(cert));

            switch (ViewModel.Format)
            {
                case CertificateFormat.Der:
                    fileName = $"{cert.Name}.cer";
                    break;
                case CertificateFormat.Pkcs12:
                    fileName = $"{cert.Name}.pfx";
                    break;
                case CertificateFormat.Pem:
                    fileName = $"{cert.Name}.pem";
                    break;
            }
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

    private IEnumerable<string> PasswordStrength(string pw)
    {
        if (string.IsNullOrWhiteSpace(pw) && ViewModel.Format != CertificateFormat.Pkcs12)
            yield break;

        if (string.IsNullOrWhiteSpace(pw) && ViewModel.Format == CertificateFormat.Pkcs12)
        {
            yield return "Password is required when format is PKCS #12.";
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
        if (ViewModel.Password != pw && ViewModel.Format == CertificateFormat.Pkcs12)
            return "Passwords must match";

        return null;
    }

    [Inject]
    public IJSRuntime JS { get; set; }

    [Inject]
    public IDialogService Dialog { get; set; }

    [Inject]
    public ICertificateService CertificateService { get; set; }

    private DownloadCertificateViewModel ViewModel { get; set; } = new();

    [CascadingParameter] private MudDialogInstance MudDialog { get; set; }

    [Parameter] public string Slug { get; set; }
}