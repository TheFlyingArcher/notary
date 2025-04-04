using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

using log4net;

#region Bouncy Castle Usings
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.X509;
#endregion

using Notary.Configuration;
using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;
using Notary.Logging;

namespace Notary.Service
{
    public class CertificateService : CryptographicEntityService<Certificate>, ICertificateService
    {
        public CertificateService(
            NotaryConfiguration config,
            IAsymmetricKeyService keyService,
            ICertificateRepository repository,
            ILog log) : base(repository, log)
        {
            Configuration = config;
            KeyService = keyService;
        }

        public async Task<Certificate> IssueCertificateAsync(CertificateRequest request)
        {
            try
            {
                string issuerDn = null;
                string crlEndpoint = null;
                BigInteger issuerSn = null;
                AsymmetricCipherKeyPair issuerKeyPair = null;
                Certificate parentCert = null;

                if (!string.IsNullOrEmpty(request.ParentCertificateSlug))
                {
                    parentCert = await GetAsync(request.ParentCertificateSlug);
                    if (parentCert == null)
                        throw new InvalidOperationException("Given certificate was not found");

                    var cert = GetX509FromPem(parentCert.Data);
                    issuerKeyPair = await KeyService.GetKeyPairAsync(parentCert.KeySlug);
                    issuerSn = cert.SerialNumber;
                    issuerDn = DistinguishedName.BuildDistinguishedName(parentCert.Subject);
                    crlEndpoint = $"{Configuration.CrlEndpoint}/{parentCert.IssuingSlug}";
                }
                var random = GetSecureRandom();
                var newKey = new AsymmetricKey
                {
                    Active = true,
                    Created = DateTime.UtcNow,
                    CreatedBySlug = request.RequestedBySlug,
                    KeyAlgorithm = request.KeyAlgorithm,
                    KeyCurve = request.Curve,
                    KeyLength = request.KeySize,
                    Name = request.Name,
                    NotAfter = request.NotAfter,
                    NotBefore = request.NotBefore
                };

                var certificateKeyPair = await GenerateKeyPair(newKey);
                var serialNumber = GenerateSerialNumber(random);
                var subject = DistinguishedName.BuildDistinguishedName(request.Subject);
                var keyUsages = new List<DerObjectIdentifier>();

                foreach (var ku in request.ExtendedKeyUsages)
                {
                    var id = new DerObjectIdentifier(ku);
                    keyUsages.Add(id);
                }

                int certKeyUsages = 0;
                foreach (var usages in request.CertificateKeyUsageFlags)
                {
                    certKeyUsages |= usages;
                }

                //Generate the certificate
                var generatedCertificate = GenerateCertificate(
                    request.SubjectAlternativeNames,
                    random,
                    request.KeyAlgorithm,
                    subject,
                    certificateKeyPair,
                    serialNumber,
                    parentCert == null ? DistinguishedName.BuildDistinguishedName(request.Subject) : DistinguishedName.BuildDistinguishedName(parentCert.Subject),
                    request.NotAfter,
                    parentCert == null ? certificateKeyPair : issuerKeyPair,
                    issuerSn,
                    request.IsCaCertificate,
                    crlEndpoint,
                    certKeyUsages,
                    keyUsages.ToArray()
                );

                var thumbprint = GetThumbprint(generatedCertificate);

                var certificate = new Certificate
                {
                    Active = true,
                    Created = DateTime.UtcNow,
                    CreatedBySlug = request.RequestedBySlug,
                    Data = await ConvertX509ToPemAsync(generatedCertificate),
                    IsCaCertificate = request.IsCaCertificate,
                    Issuer = parentCert == null ? request.Subject : parentCert.Subject,
                    IssuingSlug = request.ParentCertificateSlug,
                    ExtendedKeyUsages = [.. request.ExtendedKeyUsages],
                    KeySlug = newKey.Slug,
                    Name = request.Name,
                    NotAfter = request.NotAfter,
                    NotBefore = request.NotBefore,
                    SerialNumber = generatedCertificate.SerialNumber.ToString(16),
                    Subject = request.Subject,
                    SignatureAlgorithm = generatedCertificate.SigAlgName,
                    SubjectAlternativeNames = request.SubjectAlternativeNames,
                    Thumbprint = thumbprint
                };

                await SaveAsync(certificate, request.RequestedBySlug);

                return certificate;
            }
            catch (Exception ex)
            {
                throw ex.IfNotLoggedThenLog(Logger);
            }
        }

        public async Task<byte[]> RequestCertificateAsync(string slug, CertificateFormat format, string privateKeyPassword)
        {
            var certificate = await GetAsync(slug);
            if (certificate == null)
                return null;

            byte[] certificateBinary = null;

            X509Certificate cert = GetX509FromPem(certificate.Data);
            if (cert == null)
                return null;
            cert.CheckValidity();

            switch (format)
            {
                case CertificateFormat.Der:
                    certificateBinary = cert.GetEncoded();
                    break;
                case CertificateFormat.Pem:
                    certificateBinary = Encoding.Default.GetBytes(certificate.Data);
                    break;
                case CertificateFormat.Pkcs12:
                    var certKey = await KeyService.GetKeyPairAsync(certificate.KeySlug);
                    var store = new Pkcs12StoreBuilder().Build();
                    var certEntry = new X509CertificateEntry(cert);
                    var keyEntry = new AsymmetricKeyEntry(certKey.Private);

                    store.SetKeyEntry(certificate.Subject.ToString(), keyEntry, [certEntry]);
                    using (var memStream = new MemoryStream())
                    {
                        store.Save(memStream, privateKeyPassword.ToArray(), GetSecureRandom());
                        certificateBinary = memStream.ToArray();
                    }
                    break;
                case CertificateFormat.Pkcs7:
                    //TODO: Figure this out.
                    break;
                default:
                    throw new ArgumentException(nameof(format));
            }
            return certificateBinary;
        }

        public async Task<List<Certificate>> GetCertificatesByCaAsync(string slug)
        {
            var repository = (ICertificateRepository)Repository;

            return await repository.GetCertificatesByCaAsync(slug);
        }

        /// <summary>
        /// Add the Authority Key Identifier. According to http://www.alvestrand.no/objectid/2.5.29.35.html, this
        /// identifies the public key to be used to verify the signature on this certificate.
        /// In a certificate chain, this corresponds to the "Subject Key Identifier" on the *issuer* certificate.
        /// The Bouncy Castle documentation, at http://www.bouncycastle.org/wiki/display/JA1/X.509+Public+Key+Certificate+and+Certification+Request+Generation,
        /// shows how to create this from the issuing certificate. Since we're creating a self-signed certificate, we have to do this slightly differently.
        /// </summary>
        /// <param name="certificateGenerator">The object used to generate certificate</param>
        /// <param name="issuerDN">The issuer's distinguished name</param>
        /// <param name="issuerKeyPair">The issuer's key pair</param>
        /// <param name="issuerSerialNumber">The issuer's serial number</param>
        private void AddAuthorityKeyIdentifier(X509V3CertificateGenerator certificateGenerator,
                                                      X509Name issuerDN,
                                                      AsymmetricCipherKeyPair issuerKeyPair,
                                                      BigInteger issuerSerialNumber)
        {
            var authorityKeyIdentifierExtension =
                new AuthorityKeyIdentifier(
                    SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(issuerKeyPair.Public),
                    new GeneralNames(new GeneralName(issuerDN)),
                    issuerSerialNumber);
            certificateGenerator.AddExtension(
                X509Extensions.AuthorityKeyIdentifier.Id, false, authorityKeyIdentifierExtension);
        }

        /// <summary>
        /// Add the certificate key usage extension, e.g. Key Encipherment
        /// </summary>
        /// <param name="certificateGenerator"></param>
        /// <param name="usages"></param>
        private void AddCertificateKeyUsage(X509V3CertificateGenerator certificateGenerator, int keyUsageFlags)
        {
            certificateGenerator.AddExtension(X509Extensions.KeyUsage.Id, true, new KeyUsage(keyUsageFlags));
        }

        private void AddCrlEndpoint(X509V3CertificateGenerator certificateGenerator, string crlEndpoint)
        {
            var asn1Names = new Asn1EncodableVector();
            var gn = new GeneralName(GeneralName.UniformResourceIdentifier, new DerIA5String(crlEndpoint));
            asn1Names.Add(gn);

            var gns = GeneralNames.GetInstance(new DerSequence(asn1Names));
            var dpn = new DistributionPointName(DistributionPointName.FullName, gns);
            var dp = new DistributionPoint(dpn, null, null);

            var crlDistPoint = CrlDistPoint.GetInstance(new DerSequence(dp));

            certificateGenerator.AddExtension(X509Extensions.CrlDistributionPoints, false, crlDistPoint);
        }

        /// <summary>
        /// Add the "Extended Key Usage" extension, specifying (for example) "server authentication".
        /// </summary>
        /// <param name="certificateGenerator"></param>
        /// <param name="usages"></param>
        private void AddExtendedKeyUsage(X509V3CertificateGenerator certificateGenerator, IEnumerable<DerObjectIdentifier> usages)
        {
            certificateGenerator.AddExtension(
                X509Extensions.ExtendedKeyUsage.Id, false, new ExtendedKeyUsage(usages));
        }

        /// <summary>
        /// Add the "Basic Constraints" extension.
        /// </summary>
        /// <param name="certificateGenerator"></param>
        /// <param name="isCertificateAuthority"></param>
        private void AddBasicConstraints(X509V3CertificateGenerator certificateGenerator,
                                                bool isCertificateAuthority)
        {
            certificateGenerator.AddExtension(
                X509Extensions.BasicConstraints.Id, true, new BasicConstraints(isCertificateAuthority));
        }

        /// <summary>
        /// Add the Subject Key Identifier.
        /// </summary>
        /// <param name="certificateGenerator"></param>
        /// <param name="subjectKeyPair"></param>
        private void AddSubjectKeyIdentifier(X509V3CertificateGenerator certificateGenerator,
                                                    AsymmetricCipherKeyPair subjectKeyPair)
        {
            var subjectKeyIdentifierExtension =
                new SubjectKeyIdentifier(
                    SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(subjectKeyPair.Public));
            certificateGenerator.AddExtension(
                X509Extensions.SubjectKeyIdentifier.Id, false, subjectKeyIdentifierExtension);
        }

        /// <summary>
        /// Add the "Subject Alternative Names" extension. Note that you have to repeat
        /// the value from the "Subject Name" property.
        /// </summary>
        /// <param name="certificateGenerator"></param>
        /// <param name="subjectAlternativeNames"></param>
        private void AddSubjectAlternativeNames(X509V3CertificateGenerator certificateGenerator,
                                                       IEnumerable<SubjectAlternativeName> subjectAlternativeNames)
        {
            var sanExtension = new DerSequence(subjectAlternativeNames.Select(n =>
            {
                int generalName = -1;
                switch (n.Kind)
                {
                    case SanKind.Dns:
                        generalName = GeneralName.DnsName;
                        break;
                    case SanKind.Email:
                        generalName = GeneralName.Rfc822Name;
                        break;
                    case SanKind.IpAddress:
                        generalName = GeneralName.IPAddress;
                        break;
                    case SanKind.UserPrincipal:
                        generalName = GeneralName.DirectoryName;
                        break;
                    case SanKind.Uri:
                        generalName = GeneralName.UniformResourceIdentifier;
                        break;
                    default:
                        break;
                }

                return new GeneralName(generalName, n.Name);
            }).ToArray<Asn1Encodable>());

            certificateGenerator.AddExtension(X509Extensions.SubjectAlternativeName, false, sanExtension);
        }

        private X509Certificate GenerateCertificate(List<SubjectAlternativeName> sanList, SecureRandom random, Algorithm alg,
            string subjectDn, AsymmetricCipherKeyPair subjectKeyPair, BigInteger subjectSn, string issuerDn, DateTime notAfter,
            AsymmetricCipherKeyPair issuerKeyPair, BigInteger issuerSn, bool isCA, string crlEndpoint, int certificateKeyUsageFlags,
            IEnumerable<DerObjectIdentifier> usages)
        {
            var certGen = new X509V3CertificateGenerator();
            var subject = new X509Name(subjectDn);
            var issuer = new X509Name(issuerDn);
            var notBefore = DateTime.UtcNow;
            Asn1SignatureFactory signatureFactory = null;
            switch (alg)
            {
                case Algorithm.RSA:
                    signatureFactory = new Asn1SignatureFactory("SHA256WithRSA", issuerKeyPair.Private);
                    break;
                case Algorithm.EllipticCurve:
                    signatureFactory = new Asn1SignatureFactory("SHA256WithECDSA", issuerKeyPair.Private);
                    break;
            }

            certGen.SetPublicKey(subjectKeyPair.Public);
            certGen.SetSerialNumber(subjectSn);
            certGen.SetSubjectDN(subject);
            certGen.SetIssuerDN(issuer);
            certGen.SetNotBefore(notBefore);
            certGen.SetNotAfter(notAfter);

            AddAuthorityKeyIdentifier(certGen, issuer, issuerKeyPair, issuerSn);
            AddSubjectKeyIdentifier(certGen, subjectKeyPair);
            AddBasicConstraints(certGen, isCA);
            AddCertificateKeyUsage(certGen, certificateKeyUsageFlags);

            if (!string.IsNullOrEmpty(crlEndpoint))
                AddCrlEndpoint(certGen, crlEndpoint);

            if (usages != null && usages.Any())
                AddExtendedKeyUsage(certGen, usages);

            if (sanList != null && sanList.Any())
                AddSubjectAlternativeNames(certGen, sanList);

            var bouncyCert = certGen.Generate(signatureFactory);

            return bouncyCert;
        }

        private async Task<AsymmetricCipherKeyPair> GenerateKeyPair(AsymmetricKey newKey)
        {
            await KeyService.SaveAsync(newKey, newKey.UpdatedBySlug);

            var keyPair = await KeyService.GetKeyPairAsync(newKey.Slug);
            if (keyPair == null)
                throw new ArgumentNullException(nameof(keyPair));
            return keyPair;
        }

        /// <summary>
        /// Get the certificate thumbprint/fingerprint
        /// </summary>
        /// <param name="certificate">The certificate for finding thumbprint</param>
        /// <returns>The SHA256 thumbprint of the certificate</returns>
        private string GetThumbprint(X509Certificate certificate)
        {
            byte[] certData = certificate.GetEncoded();

            var digest = new Sha256Digest();
            digest.BlockUpdate(certData, 0, certData.Length);
            byte[] digestedCert = new byte[digest.GetDigestSize()];
            digest.DoFinal(digestedCert, 0);
            byte[] hexBytes = Hex.Encode(digestedCert);

            return Encoding.ASCII.GetString(hexBytes);
        }

        protected NotaryConfiguration Configuration { get; }

        protected IAsymmetricKeyService KeyService { get; }
    }
}
