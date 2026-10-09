using System.Collections.Generic;
using System.Threading.Tasks;
using Notary.Contract;

namespace Notary.Interface.Repository
{
    public interface IRevocatedCertificateRepository : IRepository<RevocatedCertificate>
    {
        /// <summary>
        /// Get the active revocations of certificates issued by a CA
        /// </summary>
        Task<List<RevocatedCertificate>> GetActiveByIssuerAsync(string issuerSlug);

        /// <summary>
        /// Get the active revocation of a certificate
        /// </summary>
        /// <returns>The revocation or null if the certificate is not revoked</returns>
        Task<RevocatedCertificate> GetActiveByCertificateAsync(string certificateSlug);
    }
}
