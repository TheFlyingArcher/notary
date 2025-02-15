using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Notary.Data.Model
{
    [Collection("certificate_authorities")]
    public class CertificateAuthorityModel : BaseModel
    {
        public CertificateAuthorityModel()
        {

        }

        /// <summary>
        /// Get or set slug of the certificate associated with the CA
        /// </summary>
        [BsonElement("cert_slug")]
        public string CertificateSlug { get; set; }

        /// <summary>
        /// Endpoint to the CRL
        /// </summary>
        [BsonElement("crl_endpoint")]
        public string CrlEndpoint { get; set; }

        [BsonElement("dn")]
        public DistinguishedNameModel DistinguishedName { get; set; }

        [BsonElement("is_issuer")]
        public bool IsIssuer { get; set; }

        [BsonElement("issuing_dn")]
        public DistinguishedNameModel IssuingDn { get; set; }

        [BsonElement("key_alg")]
        public Algorithm KeyAlgorithm
        {
            get; set;
        }

        /// <summary>
        /// The elliptic curve to use if EC is used to generate the keys
        /// </summary>
        [BsonElement("curve")]
        public EllipticCurve? KeyCurve { get; set; }

        /// <summary>
        /// The length of the RSA key if RSA is used to generate the keys
        /// </summary>
        [BsonElement("key_len")]
        public int? KeyLength { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("not_after")]
        public DateTime NotAfter { get; set; }

        [BsonElement("not_before")]
        public DateTime NotBefore { get; set; }

        [BsonElement("parent_slug")]
        public string ParentCaSlug { get; set; }
    }
}
