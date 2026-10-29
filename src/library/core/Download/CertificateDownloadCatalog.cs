using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Notary.Download;

/// <summary>
/// The download formats offered for a certificate, in plain language, and the rule for naming the downloaded file
/// </summary>
public static class CertificateDownloadCatalog
{
    // The file is saved on the user's machine, most often Windows, not on the server, so this is a fixed set rather
    // than Path.GetInvalidFileNameChars(), which on Linux allows everything but '/'
    private static readonly char[] _invalidFileNameChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    private static readonly CertificateDownloadOption[] _options =
    [
        new(
            CertificateFormat.Der,
            "Certificate only (.cer)",
            ".cer",
            "Just the public certificate, in binary form. Use it to import into Windows or any tool that wants a single certificate file. It does not contain the private key.",
            false,
            false),
        new(
            CertificateFormat.Pem,
            "Certificate only, text (.pem)",
            ".pem",
            "Just the public certificate, as Base64 text. Use it for Linux, nginx, OpenSSL, or pasting into a configuration file. It does not contain the private key.",
            false,
            false),
        new(
            CertificateFormat.Pkcs12,
            "Certificate + private key bundle (.pfx)",
            ".pfx",
            "The certificate, its issuing chain and its private key in one password-protected file. Use it to install the certificate on a server or move it to another machine. This is the only option that includes the private key, so choose a strong password and keep it safe.",
            true,
            true),
        new(
            CertificateFormat.Pkcs7Pem,
            "Certificate chain, text (.p7b)",
            ".p7b",
            "The certificate and every CA that issued it, as text. Use it to hand out a trust chain or with 'openssl pkcs7'. It does not contain the private key.",
            false,
            false),
        new(
            CertificateFormat.Pkcs7,
            "Certificate chain, binary (.p7b)",
            ".p7b",
            "The certificate and every CA that issued it, in binary form. Use it to import a chain into Windows. OpenSSL needs '-inform DER' to read it. It does not contain the private key.",
            false,
            false)
    ];

    /// <summary>
    /// Get every option, in the order the dialog shows them
    /// </summary>
    public static IReadOnlyList<CertificateDownloadOption> All => _options;

    /// <summary>
    /// Get the options that may be offered for a certificate. A certificate authority's private key never leaves
    /// Notary, so for one the options that include a private key are left out.
    /// </summary>
    /// <param name="isCaCertificate">Whether the certificate is a certificate authority</param>
    public static IReadOnlyList<CertificateDownloadOption> ForCertificate(bool isCaCertificate)
    {
        return isCaCertificate
            ? _options.Where(o => !o.ContainsPrivateKey).ToList()
            : _options;
    }

    /// <summary>
    /// Get the option for a format
    /// </summary>
    /// <param name="format">The format to look up</param>
    /// <exception cref="ArgumentOutOfRangeException">The format is not a download format</exception>
    public static CertificateDownloadOption For(CertificateFormat format)
    {
        var option = _options.FirstOrDefault(o => o.Format == format);
        if (option == null)
        {
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown download format");
        }

        return option;
    }

    /// <summary>
    /// Build the name of the downloaded file
    /// </summary>
    /// <param name="format">The format being downloaded, which decides the extension</param>
    /// <param name="requestedName">The name the user typed; blank to use the certificate name</param>
    /// <param name="certificateName">The certificate's own name, used when no usable name was typed</param>
    /// <returns>The file name with the extension for the format</returns>
    public static string BuildFileName(CertificateFormat format, string? requestedName, string certificateName)
    {
        var extension = For(format).Extension;
        var baseName = Sanitize(requestedName, extension);
        if (baseName.Length == 0)
        {
            baseName = Sanitize(certificateName, extension);
        }

        if (baseName.Length == 0)
        {
            baseName = "certificate";
        }

        return baseName + extension;
    }

    private static string Sanitize(string? name, string extension)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var cleaned = new string(name.Where(c => !char.IsControl(c) && !_invalidFileNameChars.Contains(c)).ToArray()).Trim();

        if (cleaned.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[..^extension.Length].TrimEnd();
        }

        return cleaned.Trim('.', ' ');
    }
}
