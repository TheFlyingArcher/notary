namespace Notary.Web.ViewModels;

public class DownloadCertificateViewModel
{
    public string? ConfirmPassword { get; set; }

    public string? FileName { get; set; }

    /// <summary>
    ///     Get or set the format requested for the certificate.
    /// </summary>
    public CertificateFormat Format { get; set; }

    /// <summary>
    ///     Get or set the certificate password if downloading with private key
    /// </summary>
    public string? Password { get; set; }
}