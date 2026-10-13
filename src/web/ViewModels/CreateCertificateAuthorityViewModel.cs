using Notary.Contract;
using Notary.Profiles;
using Notary.Validity;

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Notary.Web.ViewModels
{
    public class CreateCertificateAuthorityViewModel
    {
        private string? _caSlug;

        public CreateCertificateAuthorityViewModel()
        {
            CommonName = string.Empty;
            Country = string.Empty;
            Locale = string.Empty;
            Name = string.Empty;
            ParentCaSlug = string.Empty;
            Organization = string.Empty;
            OrganizationalUnit = string.Empty;
            StateProvince = string.Empty;

            CertificateAuthorities = new List<CertificateAuthority>();
            KeyType = Algorithm.RSA;
            KeyLength = 2048;
            Curve = EllipticCurve.P256;
        }

        public string CommonName { get; set; }

        public string Country { get; set; }

        public EllipticCurve Curve { get; set; }

        [Required]
        public bool IsIssuer { get; set; }

        public int KeyLength { get; set; }

        [Required]
        public Algorithm KeyType { get; set; }

        [Required]
        public ValidityPeriod? SelectedPeriod { get; set; }

        [Required]
        public CaPurpose? SelectedPurpose { get; set; }

        /// <summary>
        /// The key usages chosen for a Custom CA, in addition to the certificate and CRL signing every CA has
        /// </summary>
        public IReadOnlyCollection<CertificateKeyUsage> SelectedCustomKeyUsages { get; set; } = new List<CertificateKeyUsage>();

        /// <summary>
        /// The extended key usage OIDs chosen for a Custom CA
        /// </summary>
        public IReadOnlyCollection<string> SelectedCustomExtendedKeyUsages { get; set; } = new List<string>();

        public string Locale { get; set; }

        [Required, RegularExpression("[a-zA-Z0-9\\-]+", ErrorMessage = "Only alphanumerics plus dashes allowed")]
        public string Name { get; set; }

        public string? ParentCaSlug
        {
            get
            {
                return _caSlug;
            }
            set
            {
                _caSlug = value;
                if (OnCertificateAuthoritySlugChanged != null)
                {
                    OnCertificateAuthoritySlugChanged(value);
                }
            }
        }

        public string Organization { get; set; }

        public string OrganizationalUnit { get; set; }

        public string StateProvince { get; set; }

        public bool AdditionalSubjectExpanded { get; set; }

        public bool KeyAlgorithmExpanded { get; set; }

        public List<CertificateAuthority> CertificateAuthorities { get; set; }

        public CertificateAuthority? SelectedCa { get; set; }

        public event Func<string?, Task> OnCertificateAuthoritySlugChanged;
    }
}
