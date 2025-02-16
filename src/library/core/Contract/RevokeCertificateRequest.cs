namespace Notary.Contract
{
    /// <summary>
    /// Data contract for a revocation request
    /// </summary>
    public class RevokeCertificateRequest
    {
        /// <summary>
        /// Get or set the slug of the certificate
        /// </summary>
        public string Slug { get; set; }

        /// <summary>
        /// Get or set the reason for revoking the certificate
        /// </summary>
        public RevocationReason RevocationReason { get; set; }

        /// <summary>
        /// Get or set the user who is revoking the certificate
        /// </summary>
        public string UserRevocatingSlug { get; set; }
    }
}
