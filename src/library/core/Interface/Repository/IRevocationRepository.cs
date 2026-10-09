using System.Collections.Generic;
using System.Threading.Tasks;
using Notary.Contract;

namespace Notary.Interface.Repository
{
    public interface IRevocationRepository : IRepository<Revocation>
    {
        /// <summary>
        /// Get the active revocations of certificates issued by a CA
        /// </summary>
        Task<List<Revocation>> GetActiveByIssuerAsync(string issuerSlug);

        /// <summary>
        /// Get the active revocation of a certificate
        /// </summary>
        /// <returns>The revocation or null if the certificate is not revoked</returns>
        Task<Revocation> GetActiveByCertificateAsync(string certificateSlug);
    }
}
