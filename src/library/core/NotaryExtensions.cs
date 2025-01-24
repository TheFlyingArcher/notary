using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Security.Cryptography;
using System.IO;

namespace Notary
{
    public static class NotaryExtensions
    {
        public static readonly JsonSerializerSettings JsonSerializeSettings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };

        /// <summary>
        /// Compares two byte arrays to see if they are equal
        /// </summary>
        /// <returns><c>true</c>, if bytes equal was ared, <c>false</c> otherwise.</returns>
        /// <param name="lhs">Lhs.</param>
        /// <param name="rhs">Rhs.</param>
        public static unsafe bool AreBytesEqual(this byte[] lhs, byte[] rhs)
        {
            if (lhs == null || rhs == null)
                return false;

            if (lhs.Length != rhs.Length)
                return false;

            fixed (byte* p1 = lhs, p2 = rhs)
            {
                byte* x1 = p1, x2 = p2;
                int l = lhs.Length;
                for (int i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
                    if (*((long*)x1) != *((long*)x2)) return false;
                if ((l & 4) != 0) { if (*((int*)x1) != *((int*)x2)) return false; x1 += 4; x2 += 4; }
                if ((l & 2) != 0) { if (*((short*)x1) != *((short*)x2)) return false; x1 += 2; x2 += 2; }
                if ((l & 1) != 0) if (*((byte*)x1) != *((byte*)x2)) return false;
                return true;
            }
        }

        /// <summary>
        /// Get upto the first eight characters of a string. If less than 8, then returns the string
        /// </summary>
        /// <param name="lhs"></param>
        /// <returns>Up to the first eight characters of the string</returns>
        public static string FirstEight(this string lhs)
        {
            return string.IsNullOrEmpty(lhs) && lhs.Length <= 8 ? lhs : lhs.Substring(0, 8);
        }

        public static string MakeSlug(this string[] props)
        {
            if (props.Any(p => p == null))
                throw new ArgumentNullException(nameof(props), "A given property for making a slug was null");

            //If it's an array of one, just call the method below.
            if (props.Length == 1)
                return props.First().MakeSlug();

            return props.Aggregate((c, n) => c.MakeSlug() + "-" + n.MakeSlug());
        }

        public static string MakeSlug(this string prop)
        {
            prop = prop.ToLower();

            return prop;
        }

        /// <summary>
        /// Serialize a list object to JSON
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="self">The </param>
        /// <returns>A JSON string representing the serialized objects</returns>
        public static string ToJson<T>(this List<T> self) => JsonConvert.SerializeObject(self, JsonSerializeSettings);

        /// <summary>
        /// Serialize a JSON object to string
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="self"></param>
        /// <returns>A JSON string representing the serialized object</returns>
        public static string ToJson<T>(this T self) => JsonConvert.SerializeObject(self, JsonSerializeSettings);

        /// <summary>
        /// Export a public key in PEM format.
        /// 
        /// Credit to user "Iridium" at https://stackoverflow.com/questions/28406888/c-sharp-rsa-public-key-output-not-correct/28407693#28407693
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="writer"></param>
        public static void ExportPublicKey(this RSACryptoServiceProvider provider, TextWriter outputStream)
        {
            var parameters = provider.ExportParameters(false);
            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream);
                writer.Write((byte)0x30); // SEQUENCE
                using (var innerStream = new MemoryStream())
                {
                    var innerWriter = new BinaryWriter(innerStream);
                    innerWriter.Write((byte)0x30); // SEQUENCE
                    EncodeLength(innerWriter, 13);
                    innerWriter.Write((byte)0x06); // OBJECT IDENTIFIER
                    var rsaEncryptionOid = new byte[] { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x01 };
                    EncodeLength(innerWriter, rsaEncryptionOid.Length);
                    innerWriter.Write(rsaEncryptionOid);
                    innerWriter.Write((byte)0x05); // NULL
                    EncodeLength(innerWriter, 0);
                    innerWriter.Write((byte)0x03); // BIT STRING
                    using (var bitStringStream = new MemoryStream())
                    {
                        var bitStringWriter = new BinaryWriter(bitStringStream);
                        bitStringWriter.Write((byte)0x00); // # of unused bits
                        bitStringWriter.Write((byte)0x30); // SEQUENCE
                        using (var paramsStream = new MemoryStream())
                        {
                            var paramsWriter = new BinaryWriter(paramsStream);
                            EncodeIntegerBigEndian(paramsWriter, parameters.Modulus); // Modulus
                            EncodeIntegerBigEndian(paramsWriter, parameters.Exponent); // Exponent
                            var paramsLength = (int)paramsStream.Length;
                            EncodeLength(bitStringWriter, paramsLength);
                            bitStringWriter.Write(paramsStream.GetBuffer(), 0, paramsLength);
                        }
                        var bitStringLength = (int)bitStringStream.Length;
                        EncodeLength(innerWriter, bitStringLength);
                        innerWriter.Write(bitStringStream.GetBuffer(), 0, bitStringLength);
                    }
                    var length = (int)innerStream.Length;
                    EncodeLength(writer, length);
                    writer.Write(innerStream.GetBuffer(), 0, length);
                }

                var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
                outputStream.WriteLine("-----BEGIN PUBLIC KEY-----");
                for (var i = 0; i < base64.Length; i += 64)
                {
                    outputStream.WriteLine(base64, i, Math.Min(64, base64.Length - i));
                }
                outputStream.WriteLine("-----END PUBLIC KEY-----");
            }
        }

        /// <summary>
        /// Get a friendly name for the revocation reason
        /// </summary>
        /// <param name="reason">The reason given</param>
        /// <returns>Friendly name of the revocation reason</returns>
        /// <exception cref="InvalidOperationException">Throw if an invalid reason given</exception>
        public static string RevocationFriendlyName(this RevocationReason reason)
        {
            string friendlyReason;
            switch (reason)
            {
                case RevocationReason.AaCompromized:
                    friendlyReason = "RADIUS server compromized";
                    break;
                case RevocationReason.AffiliationChanged:
                    friendlyReason = "User no longer with organization";
                    break;
                case RevocationReason.CaCompromized:
                    friendlyReason = "CA compromized";
                    break;
                case RevocationReason.CertificateHold:
                    friendlyReason = "Temporary";
                    break;
                case RevocationReason.CessationOfOperation:
                    friendlyReason = "Certificate replaced";
                    break;
                case RevocationReason.KeyCompromized:
                    friendlyReason = "Private key compromized";
                    break;
                case RevocationReason.PrivilegeWithdrawn:
                    friendlyReason = "Privleges withdrawn";
                    break;
                case RevocationReason.RemoveFromCrl:
                    friendlyReason = "CA removed";
                    break;
                case RevocationReason.Superceded:
                    friendlyReason = "Name change";
                    break;
                case RevocationReason.Unspecified:
                    friendlyReason = "Unspecified reason";
                    break;
                default:
                    throw new InvalidOperationException("Incorrect revocation reason");
            }

            return friendlyReason;
        }

        private static void EncodeLength(BinaryWriter stream, int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException("length", "Length must be non-negative");
            if (length < 0x80)
            {
                // Short form
                stream.Write((byte)length);
            }
            else
            {
                // Long form
                var temp = length;
                var bytesRequired = 0;
                while (temp > 0)
                {
                    temp >>= 8;
                    bytesRequired++;
                }
                stream.Write((byte)(bytesRequired | 0x80));
                for (var i = bytesRequired - 1; i >= 0; i--)
                {
                    stream.Write((byte)(length >> (8 * i) & 0xff));
                }
            }
        }

        private static void EncodeIntegerBigEndian(BinaryWriter stream, byte[] value, bool forceUnsigned = true)
        {
            stream.Write((byte)0x02); // INTEGER
            var prefixZeros = 0;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] != 0) break;
                prefixZeros++;
            }
            if (value.Length - prefixZeros == 0)
            {
                EncodeLength(stream, 1);
                stream.Write((byte)0);
            }
            else
            {
                if (forceUnsigned && value[prefixZeros] > 0x7f)
                {
                    // Add a prefix zero to force unsigned if the MSB is 1
                    EncodeLength(stream, value.Length - prefixZeros + 1);
                    stream.Write((byte)0);
                }
                else
                {
                    EncodeLength(stream, value.Length - prefixZeros);
                }
                for (var i = prefixZeros; i < value.Length; i++)
                {
                    stream.Write(value[i]);
                }
            }
        }
    }
}
