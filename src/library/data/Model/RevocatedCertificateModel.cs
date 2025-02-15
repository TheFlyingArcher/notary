using MongoDB.Bson.Serialization.Attributes;

using Notary.Contract;

namespace Notary.Data.Model
{
    public sealed class RevocatedCertificateModel : BaseModel
    {
        public RevocatedCertificateModel()
        {
        }

        public RevocatedCertificateModel(RevocatedCertificate entity) : base(entity)
        {
            CertificateSlug = entity.CertificateSlug;
            Reason = entity.Reason;
            SerialNumber = entity.SerialNumber;
            Thumbprint = entity.Thumbprint;
        }

        public string CertificateSlug { get; set; }

        /// <summary>
        /// Get or set the reason the certificate was revoked
        /// </summary>
        [BsonElement("reason"), BsonRequired]
        public RevocationReason Reason { get; set; }
        /// <summary>
        /// Get or set the revoked certificate thumbprint
        /// </summary>
        [BsonElement("sn"), BsonRequired]
        public string SerialNumber { get; set; }

        /// <summary>
        /// Get or set the revoked certificate SHA-1 thumbprint
        /// </summary>
        [BsonElement("thumb"), BsonRequired]
        public string Thumbprint { get; set; }
    }
}
