using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Notary.Contract;
using Notary.Interface.Repository;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;

namespace Notary.Service
{
    public abstract class CryptographicEntityService<TC> : EntityService<TC>
        where TC : Entity, new()
    {
        public CryptographicEntityService(IRepository<TC> repository, ILog log) : base(repository, log)
        {
        }

        /// <summary>
        /// Generate a cryptographically secure random number
        /// </summary>
        /// <returns>The cryptographically secure random number generated object</returns>
        protected SecureRandom GetSecureRandom()
        {
            var random = new SecureRandom();
            return random;
        }

        /// <summary>
        /// Create a serial number used for certificate and other cryptography objects
        /// </summary>
        /// <param name="random">A crypto-random number generated</param>
        /// <returns>An integer value representing the serial number</returns>
        protected BigInteger GenerateSerialNumber()
        {
            var random = new SecureRandom();
            return GenerateSerialNumber(random);
        }

        /// <summary>
        /// Create a serial number used for certificate and other cryptography objects
        /// </summary>
        /// <param name="random">A crypto-random number generated</param>
        /// <returns>An integer value representing the serial number</returns>
        protected BigInteger GenerateSerialNumber(SecureRandom random)
        {
            var serialNumber =
                BigIntegers.CreateRandomInRange(
                    BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            return serialNumber;
        }

        /// <summary>
        /// Convert raw binary data to a X.509 certificate object
        /// </summary>
        /// <param name="certificateData">The raw certificate binary</param>
        /// <returns>An X.509 certificate or null if it is not on disk</returns>
        protected X509Certificate GetX509FromPem(string certificatePem)
        {
            var parser = new X509CertificateParser();
            X509Certificate cert;
            using (var memStream = new MemoryStream(Encoding.UTF8.GetBytes(certificatePem)))
            {
                cert = parser.ReadCertificate(memStream);
            }

            return cert;
        }

        protected async Task<string> ConvertX509ToPemAsync(X509Certificate certificate)
        {
            string pem = null;
            using (var stream = new MemoryStream())
            {
                using (TextWriter tw = new StreamWriter(stream))
                {
                    var pw = new PemWriter(tw);
                    pw.WriteObject(certificate);
                    await pw.Writer.FlushAsync();
                }
                byte[] certBinary = stream.ToArray();
                pem = Encoding.ASCII.GetString(certBinary);
            }
            return pem;
        }
    }
}
