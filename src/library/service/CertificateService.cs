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
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Collections;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.X509;
#endregion

using Notary.Configuration;
using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;
using Notary.Security;
using Notary.Logging;
using Notary.Profiles;
using Notary.Validity;

namespace Notary.Service
{
    public class CertificateService : CryptographicEntityService<Certificate>, ICertificateService
    {
        public CertificateService(
            NotaryConfiguration config,
            IAsymmetricKeyService keyService,
            ICertificateRepository repository,
            ICertificateAuthorityRepository certificateAuthorityRepository,
            ICrlBaseUrlProvider crlBaseUrlProvider,
            ILog log) : base(repository, log)
        {
            Configuration = config;
            KeyService = keyService;
            CertificateAuthorityRepository = certificateAuthorityRepository;
            CrlBaseUrlProvider = crlBaseUrlProvider;
        }

        [NotaryAuthorize(NotaryOperation.CertificateList)]
        public override Task<List<Certificate>> GetAllAsync() => base.GetAllAsync();

        [NotaryAuthorize(NotaryOperation.CertificateGet)]
        public override Task<Certificate> GetAsync(string slug) => base.GetAsync(slug);

        [NotaryAuthorize(NotaryOperation.CertificateRevoke)]
        public override Task DeleteAsync(string slug, string updatedBySlug) => base.DeleteAsync(slug, updatedBySlug);

        [NotaryAuthorize(NotaryOperation.CertificateCreate)]
        public override Task SaveAsync(Certificate entity, string updatedBySlug) => base.SaveAsync(entity, updatedBySlug);

        private static CertificateKeyUsage[] DecodeKeyUsages(IEnumerable<int> flags)
        {
            int combined = (flags ?? []).Aggregate(0, (all, flag) => all | flag);
            return [.. Enum.GetValues<CertificateKeyUsage>().Where(u => (combined & (int)u) != 0)];
        }

        [NotaryAuthorize(NotaryOperation.CertificateCreate)]
        public async Task<Result<Certificate>> IssueCertificateAsync(CertificateRequest request)
        {
            try
            {
                var subjectCheck = request.Subject.Check();
                if (!subjectCheck.IsSuccess)
                    return Result<Certificate>.Fail(subjectCheck.Status, subjectCheck.Error);

                if (request.PathLengthConstraint is < 0)
                    return Result<Certificate>.Fail(ResultStatus.Invalid, "The path length constraint cannot be negative.");

                if (request.PathLengthConstraint.HasValue && !request.IsCaCertificate)
                    return Result<Certificate>.Fail(ResultStatus.Invalid, "A path length constraint applies only to CA certificates.");

                var requestedKeyUsages = DecodeKeyUsages(request.CertificateKeyUsageFlags);
                var keyUsageCheck = CaProfileCatalog.CheckKeyUsagesForAlgorithm(request.KeyAlgorithm, requestedKeyUsages);
                if (!keyUsageCheck.IsSuccess)
                    return Result<Certificate>.Fail(keyUsageCheck.Status, keyUsageCheck.Error);

                string crlEndpoint = null;
                BigInteger issuerSn = null;
                AsymmetricCipherKeyPair issuerKeyPair = null;
                Certificate parentCert = null;

                if (!string.IsNullOrEmpty(request.ParentCertificateSlug))
                {
                    parentCert = await GetAsync(request.ParentCertificateSlug);
                    if (parentCert == null)
                        throw new InvalidOperationException("Given certificate was not found");

                    var validity = ValidityPolicy.CheckWithinIssuer(request.NotBefore, request.NotAfter, parentCert.NotBefore, parentCert.NotAfter);
                    if (!validity.IsSuccess)
                        return Result<Certificate>.Fail(validity.Status, validity.Error);

                    // A leaf must follow the purpose of the CA that issues it. A certificate with no CA record
                    // behind it has no purpose and is unrestricted, as before.
                    if (!request.IsCaCertificate)
                    {
                        var issuingCa = await CertificateAuthorityRepository.GetByCertificateSlugAsync(parentCert.Slug);
                        if (issuingCa != null)
                        {
                            var leafCheck = CaProfileCatalog.CheckLeaf(
                                CaProfileCatalog.Resolve(issuingCa),
                                request.KeyAlgorithm,
                                requestedKeyUsages,
                                request.ExtendedKeyUsages);
                            if (!leafCheck.IsSuccess)
                                return Result<Certificate>.Fail(leafCheck.Status, leafCheck.Error);
                        }
                    }

                    var cert = GetX509FromPem(parentCert.Data);
                    issuerKeyPair = await KeyService.GetKeyPairAsync(parentCert.KeySlug);
                    if (issuerKeyPair == null)
                        return Result<Certificate>.Fail(ResultStatus.NotFound, "The issuing certificate's key was not found.");

                    // Nothing may be weaker than the CA that issues it; checked before a key is generated or anything saved
                    var issuerSpec = KeySpecs.FromKey(issuerKeyPair.Public);
                    if (!issuerSpec.IsSuccess)
                        return Result<Certificate>.Fail(issuerSpec.Status, issuerSpec.Error);

                    var strengthCheck = KeyStrength.CheckNotWeaker(
                        issuerSpec.Value,
                        new KeySpec(request.KeyAlgorithm, request.Curve, request.KeySize));
                    if (!strengthCheck.IsSuccess)
                        return Result<Certificate>.Fail(strengthCheck.Status, strengthCheck.Error);

                    issuerSn = cert.SerialNumber;
                    crlEndpoint = BuildCrlEndpoint(parentCert.Slug);
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
                var subject = ToX509Name(request.Subject);
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
                    subject,
                    certificateKeyPair,
                    serialNumber,
                    parentCert == null ? subject : ToX509Name(parentCert.Subject),
                    request.NotAfter,
                    parentCert == null ? certificateKeyPair : issuerKeyPair,
                    issuerSn,
                    request.IsCaCertificate,
                    request.PathLengthConstraint,
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

                return Result<Certificate>.Ok(certificate);
            }
            catch (Exception ex)
            {
                throw ex.IfNotLoggedThenLog(Logger);
            }
        }

        [NotaryAuthorize(NotaryOperation.CertificateDownload)]
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
                    certificateBinary = await ExportPkcs12Async(certificate, privateKeyPassword);
                    break;
                case CertificateFormat.Pkcs7:
                    certificateBinary = await ExportPkcs7Async(certificate);
                    break;
                case CertificateFormat.Pkcs7Pem:
                    certificateBinary = await ExportPkcs7PemAsync(certificate);
                    break;
                default:
                    throw new ArgumentException(nameof(format));
            }
            return certificateBinary;
        }

        // The certificate and its issuers up to the root, parsed, certificate first
        private async Task<List<X509Certificate>> GetChainAsync(Certificate certificate)
        {
            var chain = await CertificateChainResolver.ResolveAsync(certificate, GetAsync);
            return [.. chain.Select(c => GetX509FromPem(c.Data) ?? throw new InvalidOperationException($"Certificate '{c.Slug}' could not be read."))];
        }

        private async Task<byte[]> ExportPkcs12Async(Certificate certificate, string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("A password is required to export a PKCS #12.", nameof(password));

            var keyPair = await KeyService.GetKeyPairAsync(certificate.KeySlug)
                ?? throw new InvalidOperationException($"The private key of certificate '{certificate.Slug}' was not found.");
            var chain = await GetChainAsync(certificate);

            return Pkcs12ExportBuilder.Build(keyPair.Private, chain, certificate.Subject.ToString(), password);
        }

        // A certificates-only PKCS #7 (RFC 2315 / RFC 5652 SignedData with no content and no signers)
        private async Task<byte[]> ExportPkcs7Async(Certificate certificate)
        {
            var chain = await GetChainAsync(certificate);

            var generator = new CmsSignedDataGenerator();
            generator.AddCertificates(CollectionUtilities.CreateStore(chain));

            return generator.Generate(new CmsProcessableByteArray([]), false).GetEncoded("DER");
        }

        // The same certificates-only PKCS #7 as ExportPkcs7Async, armored as PEM (RFC 7468) so that
        // "openssl pkcs7 -in file" reads it without "-inform DER"
        private async Task<byte[]> ExportPkcs7PemAsync(Certificate certificate)
        {
            var der = await ExportPkcs7Async(certificate);

            return Encoding.ASCII.GetBytes(System.Security.Cryptography.PemEncoding.WriteString("PKCS7", der) + "\n");
        }

        [NotaryAuthorize(NotaryOperation.CertificateList)]
        public async Task<List<Certificate>> GetCertificatesByCaAsync(string slug)
        {
            var repository = (ICertificateRepository)Repository;

            return await repository.GetCertificatesByCaAsync(slug);
        }

        [NotaryAuthorize(NotaryOperation.CertificateList)]
        public async Task<List<Certificate>> GetSelfSignedCertificatesAsync()
        {
            var repository = (ICertificateRepository)Repository;

            return await repository.GetSelfSignedAsync();
        }

        /// <summary>
        /// Add the Authority Key Identifier. According to http://www.alvestrand.no/objectid/2.5.29.35.html, this
        /// identifies the public key to be used to verify the signature on this certificate.
        /// In a certificate chain, this corresponds to the "Subject Key Identifier" on the *issuer* certificate.
        /// The Bouncy Castle documentation, at http://www.bouncycastle.org/wiki/display/JA1/X.509+Public+Key+Certificate+and+Certification+Request+Generation,
        /// shows how to create this from the issuing certificate. Since we're creating a self-signed certificate, we have to do this slightly differently.
        /// </summary>
        /// <param name="certificateGenerator">The object used to generate certificate</param>
        /// <param name="issuerKeyPair">The issuer's key pair</param>
        private void AddAuthorityKeyIdentifier(X509V3CertificateGenerator certificateGenerator,
                                                      AsymmetricCipherKeyPair issuerKeyPair)
        {
            // RFC 5280 4.2.1.1: only the key identifier is needed. Naming the issuer certificate (issuer DN + serial)
            // must reference the issuer's *own* issuer, and a mismatch makes validators such as OpenSSL
            // fail to find the issuing CA, which in turn prevents any revocation check.
            var authorityKeyIdentifierExtension =
                new AuthorityKeyIdentifier(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(issuerKeyPair.Public));
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

        /// <summary>
        /// Build the CRL distribution point URL for certificates issued by the given CA
        /// </summary>
        /// <param name="issuerSlug">The slug of the issuing CA certificate</param>
        /// <returns>The absolute URL or null if no valid CRL endpoint is configured</returns>
        private string BuildCrlEndpoint(string issuerSlug)
        {
            var baseUrl = CrlBaseUrlProvider.GetCrlBaseUrl()?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl)
                || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                Logger.Warn("No valid absolute http(s) CRL base URL could be determined; issued certificates will have no CRL distribution point.");
                return null;
            }

            return $"{baseUrl}/{Uri.EscapeDataString(issuerSlug)}";
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
        /// <param name="pathLengthConstraint">The maximum number of CAs that may follow this one in a chain; null for no limit</param>
        private void AddBasicConstraints(X509V3CertificateGenerator certificateGenerator,
                                                bool isCertificateAuthority,
                                                int? pathLengthConstraint)
        {
            var constraints = isCertificateAuthority && pathLengthConstraint.HasValue
                ? new BasicConstraints(pathLengthConstraint.Value)
                : new BasicConstraints(isCertificateAuthority);

            certificateGenerator.AddExtension(
                X509Extensions.BasicConstraints.Id, true, constraints);
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

        /// <summary>
        /// Build the certificate name from its attributes. This avoids writing the name as text and parsing it back,
        /// which can alter values that contain punctuation.
        /// </summary>
        private static X509Name ToX509Name(DistinguishedName name)
        {
            var attributes = name.GetAttributes();

            return new X509Name(
                attributes.Select(a => new DerObjectIdentifier(a.Oid)).ToList(),
                attributes.Select(a => a.Value).ToList());
        }

        private X509Certificate GenerateCertificate(List<SubjectAlternativeName> sanList, SecureRandom random,
            X509Name subject, AsymmetricCipherKeyPair subjectKeyPair, BigInteger subjectSn, X509Name issuer, DateTime notAfter,
            AsymmetricCipherKeyPair issuerKeyPair, BigInteger issuerSn, bool isCA, int? pathLengthConstraint, string crlEndpoint,
            int certificateKeyUsageFlags, IEnumerable<DerObjectIdentifier> usages)
        {
            var certGen = new X509V3CertificateGenerator();
            var notBefore = DateTime.UtcNow;
            // The issuer's key signs, so its type and strength choose the algorithm, not the subject's key
            var signatureFactory = new Asn1SignatureFactory(
                SignatureAlgorithms.ForIssuerKey(issuerKeyPair.Private), issuerKeyPair.Private);

            certGen.SetPublicKey(subjectKeyPair.Public);
            certGen.SetSerialNumber(subjectSn);
            certGen.SetSubjectDN(subject);
            certGen.SetIssuerDN(issuer);
            certGen.SetNotBefore(notBefore);
            certGen.SetNotAfter(notAfter);

            AddAuthorityKeyIdentifier(certGen, issuerKeyPair);
            AddSubjectKeyIdentifier(certGen, subjectKeyPair);
            AddBasicConstraints(certGen, isCA, pathLengthConstraint);

            // RFC 5280 requires at least one bit when the extension is present; an empty one is rejected by OpenSSL 3.x
            if (certificateKeyUsageFlags != 0)
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

        protected ICertificateAuthorityRepository CertificateAuthorityRepository { get; }

        protected ICrlBaseUrlProvider CrlBaseUrlProvider { get; }
    }
}
