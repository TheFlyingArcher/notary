using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Notary.Contract;

namespace Notary.Data.Model
{
    [BsonIgnoreExtraElements]
    public class DistinguishedNameModel
    {
        public DistinguishedNameModel()
        {

        }

        [BsonElement("CN"), BsonIgnoreIfDefault, BsonIgnoreIfNull]
        public string CommonName
        {
            get; set;
        }

        [BsonElement("C"), BsonIgnoreIfDefault, BsonIgnoreIfNull]
        public string Country
        {
            get; set;
        }

        [BsonElement("L"), BsonIgnoreIfDefault, BsonIgnoreIfNull]
        public string Locale
        {
            get; set;
        }

        [BsonElement("O"), BsonIgnoreIfDefault, BsonIgnoreIfNull]
        public string Organization
        {
            get; set;
        }

        [BsonElement("OU"), BsonIgnoreIfDefault, BsonIgnoreIfNull]
        public string OrganizationalUnit
        {
            get; set;
        }

        [BsonElement("S"), BsonIgnoreIfDefault, BsonIgnoreIfNull]
        public string StateProvince
        {
            get; set;
        }
    }
}
