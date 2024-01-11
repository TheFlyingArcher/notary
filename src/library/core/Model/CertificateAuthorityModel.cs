﻿using MongoDB.Bson.Serialization.Attributes;

using Notary.Contract;

using System;

namespace Notary.Model
{
    [Collection("certificate_authorities")]
    public class CertificateAuthorityModel : BaseModel
    {
        public CertificateAuthorityModel()
        {

        }

        public CertificateAuthorityModel(CertificateAuthority ca) : base(ca)
        {
            CertificateSlug = ca.CertificateSlug;
            DistinguishedName = new DistinguishedNameModel(ca.DistinguishedName);
            if (ca.IssuingDn != null)
                IssuingDn = new DistinguishedNameModel(ca.IssuingDn);
            IsIssuer = ca.IsIssuer;
            KeyAlgorithm = ca.KeyAlgorithm;
            KeyCurve = ca.KeyCurve;
            KeyLength = ca.KeyLength;
            Name = ca.Name;
            NotAfter = ca.NotAfter;
            NotBefore = ca.NotBefore;
            ParentCaSlug = ca.ParentCaSlug;
        }

        /// <summary>
        /// Get or set slug of the certificate associated with the CA
        /// </summary>
        [BsonElement("cert_slug")]
        public string CertificateSlug { get; set; }

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
