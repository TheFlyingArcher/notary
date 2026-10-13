using Notary.Contract;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Notary.Interface.Service
{
    public interface ICertificateAuthorityService : IEntityService<CertificateAuthority>
    {
        /// <summary>
        /// Get a brief list of certificate authorities
        /// </summary>
        /// <returns>A brief list of certificate authorities</returns>
        Task<List<CaBrief>> GetCaListBrief();

        /// <summary>
        /// Get the certificate authorities that can issue leaf certificates: issuers whose purpose profile
        /// allows leaf certificates and whose CA certificate is not revoked.
        /// </summary>
        /// <returns>The matching certificate authorities, ordered by name</returns>
        Task<List<CertificateAuthority>> GetLeafIssuingAuthoritiesAsync();

        /// <summary>
        /// Create a certificate authority and its certificate
        /// </summary>
        /// <param name="entity">The certificate authority to create</param>
        /// <param name="updatedBySlug">The slug of the creating user</param>
        /// <returns>The created CA, or an Invalid result if its validity falls outside the parent CA's</returns>
        Task<Result<CertificateAuthority>> CreateAsync(CertificateAuthority entity, string updatedBySlug);
    }
}
