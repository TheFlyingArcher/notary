using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Notary.Profiles;

namespace Notary.Contract
{
    public class CertificateAuthority : Entity
    {
        public CertificateAuthority()
        {

        }

        /// <summary>
        /// Get or set slug of the certificate associated with the CA
        /// </summary>
        [JsonProperty("certSlug", Required = Required.Always)]
        public string CertificateSlug { get; set; }

        /// <summary>
        /// Get or set the CA distinguished name
        /// </summary>
        [JsonProperty("dn", Required = Required.Always)]
        public DistinguishedName DistinguishedName { get; set; }

        [JsonProperty("isIssuer", Required = Required.Always)]
        public bool IsIssuer { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("issDn", Required = Required.Always)]
        public DistinguishedName IssuingDn { get; set; }

        /// <summary>
        /// Asymmetric key algorithm
        /// </summary>
        [JsonProperty("ka", Required = Required.Always)]
        public Algorithm KeyAlgorithm
        {
            get; set;
        }

        /// <summary>
        /// The elliptic curve to use if EC is used to generate the keys
        /// </summary>
        [JsonProperty("curve", Required = Required.AllowNull)]
        public EllipticCurve? KeyCurve { get; set; }

        /// <summary>
        /// The length of the RSA key if RSA is used to generate the keys
        /// </summary>
        [JsonProperty("keyLen", Required = Required.AllowNull)]
        public int? KeyLength { get; set; }

        /// <summary>
        /// The name of the certificate authority
        /// </summary>
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// The CA expiration
        /// </summary>
        [JsonProperty("na", Required = Required.Always)]
        public DateTime NotAfter { get; set; }

        /// <summary>
        /// The CA validity start
        /// </summary>
        [JsonProperty("nb", Required = Required.Always)]
        public DateTime NotBefore { get; set; }

        /// <summary>
        /// The parent CA slug
        /// </summary>
        [JsonProperty("parentCaSlug", Required = Required.Always)]
        public string ParentCaSlug { get; set; }

        /// <summary>
        /// What the CA is for. Null on CAs created before purposes existed, which are treated as Custom.
        /// </summary>
        [JsonProperty("purpose", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public CaPurpose? Purpose { get; set; }

        /// <summary>
        /// The key usages chosen for a Custom CA; unused for other purposes
        /// </summary>
        [JsonProperty("customKu", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public List<CertificateKeyUsage> CustomKeyUsages { get; set; }

        /// <summary>
        /// The extended key usage OIDs chosen for a Custom CA; unused for other purposes
        /// </summary>
        [JsonProperty("customEku", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public List<string> CustomExtendedKeyUsages { get; set; }

        public override string[] SlugProperties()
        {
            return new string[] { Guid.NewGuid().ToString() };
        }
    }
}
