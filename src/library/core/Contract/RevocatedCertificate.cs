using System.Runtime.Serialization;

namespace Notary.Contract
{
    /// <summary>
    /// A contract for revoked certificate records
    /// </summary>
    [DataContract]
    public class RevocatedCertificate : Entity
    {
        public RevocatedCertificate()
        {

        }

        /// <summary>
        /// Get or set the slug of the certificate that was revoked.
        /// </summary>
        public string CertificateSlug { get; set; }

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
