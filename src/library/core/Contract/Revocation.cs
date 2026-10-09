using System;
using System.Runtime.Serialization;

namespace Notary.Contract
{
    /// <summary>
    /// A contract for revoked certificate records
    /// </summary>
    [DataContract]
    public class Revocation : Entity
    {
        public Revocation()
        {

        }

        /// <summary>
        /// Get or set the slug of the certificate that was revoked.
        /// </summary>
        public string CertificateSlug { get; set; }

        /// <summary>
        /// Get or set the slug of the CA that issued the revoked certificate. Null for self-signed roots.
        /// </summary>
        [DataMember]
        public string IssuerSlug { get; set; }

        /// <summary>
        /// Get or set when the certificate was revoked (UTC)
        /// </summary>
        [DataMember]
        public DateTime RevocationDate { get; set; }

        /// <summary>
        /// Get or set the date the certificate is known or suspected to have been compromised (UTC)
        /// </summary>
        [DataMember]
        public DateTime? InvalidityDate { get; set; }

        /// <summary>
        /// Get or set the reason the certificate was revoked
        /// </summary>
        [DataMember]
        public RevocationReason Reason { get; set; }
        /// <summary>
        /// Get or set the revoked certificate thumbprint
        /// </summary>
        [DataMember]
        public string SerialNumber { get; set; }

        /// <summary>
        /// Get or set the revoked certificate SHA-1 thumbprint
        /// </summary>
        [DataMember]
        public string Thumbprint { get; set; }

        public override string[] SlugProperties()
        {
            return new string[]
            {
                Thumbprint
            };
        }
    }
}
