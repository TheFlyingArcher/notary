using System;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace Notary.Contract
{
    public sealed class AsymmetricKey : Entity
    {
        public AsymmetricKey() { }

        public AsymmetricKey(byte[] publicKey, byte[] privateKey)
        {
            PublicKey = publicKey;
            EncryptedPrivateKey = privateKey;
        }

        [JsonProperty("enc_prv_key", Required = Required.Always)]
        public byte[] EncryptedPrivateKey { get; set; }

        [JsonProperty("alg", Required = Required.Always)]
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
        [JsonProperty("key_len", Required = Required.AllowNull)]
        public int? KeyLength { get; set; }

        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty("nb", DefaultValueHandling = DefaultValueHandling.Populate, Required = Required.Always)]
        public DateTime NotBefore { get; set; }

        [JsonProperty("na", DefaultValueHandling = DefaultValueHandling.Populate, Required = Required.Always)]
        public DateTime NotAfter { get; set; }

        [JsonProperty("pub_key", Required = Required.Always)]
        public byte[] PublicKey { get; set; }

        public override string[] SlugProperties()
        {
            return new string[] { Guid.NewGuid().ToString("N") };
        }
    }
}
