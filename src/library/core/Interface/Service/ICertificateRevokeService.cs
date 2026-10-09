using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Notary.Contract;

namespace Notary.Interface.Service
{
    public interface ICertificateRevokeService : IEntityService<Revocation>
    {
        /// <summary>
        /// Get the current CRL of a certificate authority. A cached CRL is returned while it is fresh;
        /// otherwise a new one is issued.
        /// </summary>
        /// <param name="caSlug">The slug of the issuing CA certificate</param>
        Task<Result<CrlDocument>> GetCrlAsync(string caSlug, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a list of all active revocations
        /// </summary>
        Task<List<Revocation>> GetRevocations();

        /// <summary>
        /// Get the active revocation of a certificate
        /// </summary>
        /// <returns>The revocation, or null if the certificate is not revoked</returns>
        Task<Revocation> GetRevocationAsync(string certificateSlug);

        /// <summary>
        /// Revoke a certificate. Revoking a CA (for any reason except <see cref="RevocationReason.CertificateHold"/>)
        /// also revokes every certificate it issued, directly or transitively, with
        /// <see cref="RevocationReason.CaCompromized"/>.
        /// </summary>
        /// <param name="slug">The slug of the certificate to revoke</param>
        /// <param name="reason">The reason for its revocation</param>
        /// <param name="userRevocatingSlug">The user revoking the certificate</param>
        /// <param name="invalidityDate">When the key was compromised, if known. Defaults to now for compromise reasons.</param>
        /// <returns>The number of certificates revoked, including cascaded ones</returns>
        Task<Result<int>> RevokeCertificateAsync(
            string slug,
            RevocationReason reason,
            string userRevocatingSlug,
            DateTime? invalidityDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lift a <see cref="RevocationReason.CertificateHold"/>. Other revocations are permanent.
        /// </summary>
        Task<Result> ReinstateCertificateAsync(string slug, string userSlug, CancellationToken cancellationToken = default);
    }
}
