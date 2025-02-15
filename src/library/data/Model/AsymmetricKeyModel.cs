using System;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Notary.Contract;

namespace Notary.Data.Model;

public class AsymmetricKeyModel : BaseModel
{
    public AsymmetricKeyModel() { }

    public AsymmetricKeyModel(AsymmetricKey key) : base(key)
    {
        EncryptedPrivateKey = Encoding.Default.GetString(key.EncryptedPrivateKey);
        KeyAlgorithm = key.KeyAlgorithm;
        KeyCurve = key.KeyCurve;
        KeyLength = key.KeyLength;
        Name = key.Name;
        NotAfter = key.NotAfter;
        NotBefore = key.NotBefore;
    }

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
