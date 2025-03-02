using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Notary.Contract;

namespace Notary.Interface.Service
{
    public interface ICertificateRevokeService : IEntityService<RevocatedCertificate>
    {
        /// <summary>
        /// Generate a Certificate Revocation List
        /// </summary>
        /// <returns>A CRL in PEM format</returns>
        Task<byte[]> GenerateCrl(string caSlug);

        /// <summary>
        /// Get a list of all revocated certificates
        /// </summary>
        /// <returns>A list of all certificates revocated</returns>
        Task<List<RevocatedCertificate>> GetRevocatedCertificates();

        /// <summary>
        /// Revoke a certificate
        /// </summary>
        /// <param name="thumbprint">The certificate thumbprint</param>
        /// <param name="reason">The reason for its revocation</param>
        Task RevokeCertificateAsync(string slug, RevocationReason reason, string userRevocatingSlug);
    }
}
