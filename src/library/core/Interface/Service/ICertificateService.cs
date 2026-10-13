using Notary.Contract;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Notary.Interface.Service
{
    /// <summary>
    /// Defines means to store and configure certificates
    /// </summary>
    public interface ICertificateService : IEntityService<Certificate>
    {
        /// <summary>
        /// Creates new certificate
        /// </summary>
        /// <param name="request">The certificate request</param>
        /// <returns>The issued certificate, or an Invalid result if its validity falls outside the issuing CA's</returns>
        Task<Result<Certificate>> IssueCertificateAsync(CertificateRequest request);

        /// <summary>
        /// Request a certificate from the CA in various formats
        /// </summary>
        /// <param name="thumbprint">The thumbprint</param>
        /// <param name="format">The format to download</param>
        /// <returns>The certificate binary data</returns>
        Task<byte[]> RequestCertificateAsync(string slug, CertificateFormat format, string privateKeyPassword);

        /// <summary>
        /// Get all certificates belonging to the CA
        /// </summary>
        /// <param name="slug">The slug of the CA's certificate (the certificate's issuing slug)</param>
        /// <returns>List of certificates in the Certificate Authority</returns>
        Task<List<Certificate>> GetCertificatesByCaAsync(string slug);

        /// <summary>
        /// Get the self-signed, non-CA certificates
        /// </summary>
        /// <returns>List of certificates that have no issuing certificate</returns>
        Task<List<Certificate>> GetSelfSignedCertificatesAsync();
    }
}
