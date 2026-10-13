using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Notary.Contract;

namespace Notary.Interface.Repository
{
    public interface ICertificateRepository : IRepository<Certificate>
    {
        /// <summary>
        /// Get all the certificates issued by the CA.
        /// </summary>
        /// <param name="caSlug">The slug of the CA's certificate (the certificate's issuing slug)</param>
        /// <returns>List of certificates issued by the CA</returns>
        Task<List<Certificate>> GetCertificatesByCaAsync(string caSlug);

        /// <summary>
        /// Get the self-signed, non-CA certificates (those with no issuing certificate).
        /// </summary>
        /// <returns>List of self-signed certificates</returns>
        Task<List<Certificate>> GetSelfSignedAsync();
    }
}
