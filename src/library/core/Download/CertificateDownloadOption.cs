namespace Notary.Download;

/// <summary>
/// How a certificate format is presented in the download dialog
/// </summary>
/// <param name="Format">The format this option downloads</param>
/// <param name="Label">The friendly name shown on the radio button</param>
/// <param name="Extension">The file extension, with its leading dot</param>
/// <param name="Blurb">What the file contains and common uses</param>
/// <param name="RequiresPassword">True if the download is password protected, so the user must supply a password</param>
/// <param name="ContainsPrivateKey">True if the private key is part of the download</param>
public sealed record CertificateDownloadOption(
    CertificateFormat Format,
    string Label,
    string Extension,
    string Blurb,
    bool RequiresPassword,
    bool ContainsPrivateKey);
