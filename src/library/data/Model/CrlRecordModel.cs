using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Notary.Data.Model
{
    [Collection("crl_records")]
    public sealed class CrlRecordModel : BaseModel
    {
        [BsonElement("ca_slug"), BsonRequired]
        public string CaSlug { get; set; }

        [BsonElement("number"), BsonRequired]
        public long Number { get; set; }

        [BsonElement("this_update"), BsonRequired]
        public DateTime ThisUpdate { get; set; }

        [BsonElement("next_update"), BsonRequired]
        public DateTime NextUpdate { get; set; }

        [BsonElement("entries")]
        public int EntryCount { get; set; }

        [BsonElement("data")]
        public string Data { get; set; }
    }
}
