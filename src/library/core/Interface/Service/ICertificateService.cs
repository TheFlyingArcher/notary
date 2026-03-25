using System.Collections.Generic;
using System.Threading.Tasks;
using Notary.Contract;

namespace Notary.Interface.Service;

/// <summary>
///     Defines means to store and configure certificates
/// </summary>
public interface ICertificateService : IEntityService<Certificate>
{
    /// <summary>
    ///     Creates new certificate
    /// </summary>
    /// <param name="certificate"></param>
    /// <returns></returns>
    Task<Certificate> IssueCertificateAsync(CertificateRequest request);

    /// <summary>
    ///     Request a certificate from the CA in various formats
    /// </summary>
    /// <param name="thumbprint">The thumbprint</param>
    /// <param name="format">The format to download</param>
    /// <returns>The certificate binary data</returns>
    Task<byte[]> RequestCertificateAsync(string slug, CertificateFormat format, string privateKeyPassword);

    /// <summary>
    ///     Get all certificates belonging to the CA
    /// </summary>
    /// <param name="slug">The CA slug</param>
    /// <returns>List of certificates in the Certificate Authority</returns>
    Task<List<Certificate>> GetCertificatesByCaAsync(string slug);
}