using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Newtonsoft.Json;

namespace Notary.Contract
{
    public class CertificateRequest
    {
        public CertificateRequest()
        {
        }

        [JsonProperty("keyUsageFlags", Required = Required.Always)]
        public IEnumerable<int> CertificateKeyUsageFlags { get; set; }

        [JsonProperty("crl", Required = Required.Always)]
        public string CrlEndpoint { get; set; }

        [JsonProperty("curve", Required = Required.AllowNull)]
        public EllipticCurve? Curve { get; set; }

        [JsonProperty("isCa", Required = Required.Always)]
        public bool IsCaCertificate { get; set; }

        [JsonProperty("alg", Required = Required.Always)]
        public Algorithm KeyAlgorithm { get; set; }

        [JsonProperty("keySize", Required = Required.AllowNull)]
        public int? KeySize { get; set; }

        [JsonProperty("eku", Required = Required.Always)]
        public IEnumerable<string> ExtendedKeyUsages { get; set; }

        /// <summary>
        /// Get or set the display name of the certificate
        /// </summary>
        [JsonProperty("eku", Required = Required.Always)]
        [RegularExpression("[a-zA-Z0-9\\s]+", ErrorMessage = "Only alphanumerics plus spaces allowed")]
        public string Name { get; set; }

        [JsonProperty("na", Required = Required.Always)]
        public DateTime NotAfter { get; set; }

        [JsonProperty("nb", Required = Required.Always)]
        public DateTime NotBefore { get; set; }

        [JsonProperty("parentSlug", Required = Required.Always)]
        public string ParentCertificateSlug { get; set; }

        [JsonProperty("dn", Required = Required.Always)]
        public DistinguishedName Subject { get; set; }

        /// <summary>
        /// Get a list of SAN for the certificate
        /// </summary>
        [JsonProperty("san", Required = Required.Always)]
        public List<SubjectAlternativeName> SubjectAlternativeNames
        {
            get; set;
        }

        /// <summary>
        /// Get or set the account slug that requested this certificate
        /// </summary>
        [JsonProperty("requestedBy", Required = Required.Always)]
        public string RequestedBySlug { get; set; }
    }
}
