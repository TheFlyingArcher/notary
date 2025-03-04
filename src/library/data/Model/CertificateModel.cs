﻿using System;
using System.Collections.Generic;

using MongoDB.Bson.Serialization.Attributes;

namespace Notary.Data.Model
{
    public sealed class CertificateModel : BaseModel
    {
        public CertificateModel()
        {

        }

        [BsonElement("cert_key_usages"), BsonRequired]
        public List<string> CertificateKeyUsage { get; set; }

        [BsonElement("data"), BsonRequired]
        public string Data { get; set; }

        [BsonElement("ex_key_usages"), BsonRequired]
        public List<string> ExtendedKeyUsages { get; set; }

        [BsonElement("is_ca"), BsonRequired]
        public bool IsCaCertificate { get; set; }

        [BsonElement("iss"), BsonRequired]
        public DistinguishedNameModel Issuer { get; set; }

        [BsonElement("iss_slug"), BsonRequired]
        public string IssuingSlug { get; set; }

        [BsonElement("key_slug"), BsonRequired]
        public string KeySlug { get; set; }

        [BsonElement("name"), BsonRequired]
        public string Name { get; set; }

        [BsonElement("nb"), BsonRequired]
        public DateTime NotBefore { get; set; }

        [BsonElement("na"), BsonRequired]
        public DateTime NotAfter { get; set; }

        [BsonElement("rd"), BsonIgnoreIfDefault, BsonIgnoreIfNull]
        public DateTime? RevocationDate { get; set; }

        [BsonElement("sn"), BsonRequired]
        public string SerialNumber { get; set; }

        [BsonElement("sig_alg"), BsonRequired]
        public string SignatureAlgorithm { get; set; }

        [BsonElement("sub"), BsonRequired]
        public DistinguishedNameModel Subject { get; set; }

        [BsonElement("san"), BsonIgnoreIfDefault, BsonIgnoreIfNull]
        public List<SanModel> SubjectAlternativeNames { get; set; }

        [BsonElement("thumb"), BsonRequired]
        public string Thumbprint { get; set; }
    }
}
