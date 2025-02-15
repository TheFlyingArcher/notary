using System;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace Notary.Data.Model
{
    [BsonIgnoreExtraElements]
    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(
        typeof(CertificateModel),
        typeof(CertificateAuthorityModel),
        typeof(AsymmetricKeyModel),
        typeof(RevocatedCertificateModel)
    )]
    public abstract class BaseModel
    {
        protected BaseModel()
        {

        }

        [BsonIgnoreIfDefault, BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        public ObjectId Id { get; set; }

        [BsonElement("slug"), BsonRequired]
        public string Slug { get; set; }

        [BsonElement("created"), BsonRequired]
        public DateTime Created { get; set; }

        [BsonElement("createdBy"), BsonRequired]
        public string CreatedBy { get; set; }

        [BsonElement("updated")]
        public DateTime? Updated { get; set; }

        [BsonElement("updatedBy")]
        public string UpdatedBy { get; set; }

        [BsonElement("active"), BsonRequired]
        public bool Active { get; set; }
    }
}
