using System;

using MongoDB.Bson.Serialization.Attributes;

namespace Notary.Data.Model;

public class AsymmetricKeyModel : BaseModel
{
    public AsymmetricKeyModel() { }

    [BsonElement("enc_prv_key")]
    public string EncryptedPrivateKey { get; set; }

    [BsonElement("alg")]
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

    [BsonElement("na")]
    public DateTime NotAfter { get; set; }

    [BsonElement("nb")]
    public DateTime NotBefore { get; set; }
}
