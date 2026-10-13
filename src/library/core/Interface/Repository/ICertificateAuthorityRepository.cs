using Notary.Contract;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Notary.Interface.Repository
{
    public interface ICertificateAuthorityRepository : IRepository<CertificateAuthority>
    {
        /// <summary>
        /// Get the CA that owns a certificate
        /// </summary>
        /// <param name="certificateSlug">The slug of the CA's certificate</param>
        /// <returns>The CA, or null if the certificate does not belong to a CA record</returns>
        Task<CertificateAuthority> GetByCertificateSlugAsync(string certificateSlug);
    }
}
