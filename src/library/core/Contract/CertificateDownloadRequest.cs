using System.Runtime.Serialization;

namespace Notary.Contract
{
    [DataContract]
    public class CertificateDownloadRequest
    {
        public CertificateDownloadRequest()
        {
        }

        /// <summary>
        /// Get or set the format requested for the certificate.
        /// </summary>
        public CertificateFormat Format { get; set; }

        /// <summary>
        /// Get or set the certificate password if downloading with private key
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Get or set the slug used to retrieve certificate
        /// </summary>
        public string Slug { get; set; }
    }
}
