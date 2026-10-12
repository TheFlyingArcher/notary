using MudBlazor;
using Notary.Contract;

namespace Notary.Web.ViewModels
{
    public class CertificateViewModel
    {
        public CertificateViewModel()
        {
            Issuer = new DistinguishedName();
            Issuers = new List<TreeItemData<CertificateIssuerTreeItem>>();
            KeyUsages = new List<string>();
            Name = string.Empty;
            RevocationReason = string.Empty;
            SerialNumber = string.Empty;
            SignatureAlgorithm = string.Empty;
            Subject = new DistinguishedName();
            SubjectAlternativeNames = new List<SubjectAlternativeName>();
            Thumbprint = string.Empty;
        }

        /// <summary>
        /// Get or set the CRL number of the CRL currently published for this CA
        /// </summary>
        public long? CrlNumber { get; set; }

        /// <summary>
        /// Get or set the date the published CRL must be replaced by (CAs only)
        /// </summary>
        public DateTime? CrlNextUpdate { get; set; }

        /// <summary>
        /// Get or set why no CRL could be shown for this CA, if that is the case
        /// </summary>
        public string CrlUnavailableReason { get; set; }

        /// <summary>
        /// Get or set the URL the CRL for certificates issued by this CA is published at
        /// </summary>
        public string CrlUrl { get; set; }

        public EllipticCurve? EllipticCurve { get; set; }

        public bool Expired { get; set; }

        /// <summary>
        /// Get or set whether this certificate is a certificate authority
        /// </summary>
        public bool IsCaCertificate { get; set; }

        /// <summary>
        /// Get or set whether the certificate is only temporarily revoked and can be reinstated
        /// </summary>
        public bool IsOnHold { get; set; }

        public bool Expiring { get; set; }

        public DistinguishedName Issuer
        {
            get; set;
        }

        public List<TreeItemData<CertificateIssuerTreeItem>> Issuers { get; }

        public Algorithm KeyAlgorithm { get; set; }

        public List<string> KeyUsages
        {
            get; set;
        }

        public string Name { get; set; }

        public DateTime NotAfter
        {
            get; set;
        }

        public DateTime NotBefore
        {
            get; set;
        }

        /// <summary>
        /// Get or set the date the certificate was issued
        /// </summary>
        public DateTime? RevocationDate
        {
            get; set;
        }

        public string RevocationReason { get; set; }

        public int? RsaKeyLength { get; set; }

        public string SerialNumber
        {
            get; set;
        }

        public string SignatureAlgorithm { get; set; }

        public DistinguishedName Subject { get; set; }

        public List<SubjectAlternativeName> SubjectAlternativeNames
        {
            get; set;
        }

        public string Thumbprint
        {
            get; set;
        }
    }
}
