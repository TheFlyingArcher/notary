using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Notary.Configuration;
using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;

namespace Notary.Service
{
    internal class CertificateRevokeService : CryptographicEntityService<RevocatedCertificate>, ICertificateRevokeService
    {
        public CertificateRevokeService(
            IRevocatedCertificateRepository revocatedCertificateRepo,
            ICertificateAuthorityService caService,
            ICertificateService certificateService,
            IAsymmetricKeyService keyService,
            ILog log,
            NotaryConfiguration config
        ) : base(revocatedCertificateRepo, log)
        {
            CertificateAuthority = caService;
            CertificateService = certificateService;
            Configuration = config;
            KeyService = keyService;
        }

        public async Task<byte[]> GenerateCrl(string caSlug)
        {
            var caCert = await CertificateService.GetAsync(caSlug);
            if (caCert == null)
                throw new ArgumentNullException(nameof(caCert));

            var keyInfo = await KeyService.GetAsync(caCert.KeySlug);
            var keyPair = await KeyService.GetKeyPairAsync(caCert.KeySlug);
            var signingCertificate = GetX509FromPem(caCert.Data);
            var crlGen = new X509V2CrlGenerator();

            var revocatedCerts = await GetRevocatedCertificates();

            crlGen.SetIssuerDN(signingCertificate.SubjectDN);
            crlGen.SetThisUpdate(DateTime.UtcNow);
            crlGen.SetNextUpdate(DateTime.UtcNow.AddDays(30));

            foreach (var cert in revocatedCerts)
            {
                var rc = await CertificateService.GetAsync(cert.CertificateSlug);
                var certificate = GetX509FromPem(rc.Data);
                crlGen.AddCrlEntry(certificate.SerialNumber, cert.Created, MapRevocationReason(cert.Reason));
            }

            // CRL number uses Unix epoch seconds — always increases between CRL issuances
            crlGen.AddExtension(X509Extensions.CrlNumber, false,
                new CrlNumber(BigInteger.ValueOf(DateTimeOffset.UtcNow.ToUnixTimeSeconds())));
            crlGen.AddExtension(X509Extensions.AuthorityKeyIdentifier, false,
                new AuthorityKeyIdentifierStructure(signingCertificate));

            var signatureAlgorithm = keyInfo.KeyAlgorithm == Algorithm.EllipticCurve
                ? "SHA256WithECDSA"
                : "SHA256WithRSA";

            var signatureFactory = new Asn1SignatureFactory(signatureAlgorithm, keyPair.Private);
            var crl = crlGen.Generate(signatureFactory);

            crl.Verify(signingCertificate.GetPublicKey());

            return crl.GetEncoded();
        }

        private static int MapRevocationReason(RevocationReason reason)
        {
            return reason switch
            {
                RevocationReason.Unspecified        => CrlReason.Unspecified,
                RevocationReason.KeyCompromized     => CrlReason.KeyCompromise,
                RevocationReason.CaCompromized      => CrlReason.CACompromise,
                RevocationReason.AffiliationChanged => CrlReason.AffiliationChanged,
                RevocationReason.Superceded         => CrlReason.Superseded,
                RevocationReason.CessationOfOperation => CrlReason.CessationOfOperation,
                RevocationReason.CertificateHold    => CrlReason.CertificateHold,
                RevocationReason.RemoveFromCrl      => CrlReason.RemoveFromCrl,
                RevocationReason.PrivilegeWithdrawn => CrlReason.PrivilegeWithdrawn,
                RevocationReason.AaCompromized      => CrlReason.AACompromise,
                _                                   => CrlReason.Unspecified
            };
        }

        public async Task<List<RevocatedCertificate>> GetRevocatedCertificates()
        {
            var revocatedCerts = await Repository.GetAllAsync();

            return revocatedCerts;
        }

        public async Task RevokeCertificateAsync(string slug, RevocationReason reason, string userRevocatingSlug)
        {
            var certificate = await CertificateService.GetAsync(slug);

            if (certificate != null)
            {
                certificate.RevocationDate = DateTime.UtcNow;
                await CertificateService.SaveAsync(certificate, userRevocatingSlug);

                var revocatedCertificate = new RevocatedCertificate
                {
                    Active = true,
                    CertificateSlug = slug,
                    Created = DateTime.Now,
                    CreatedBySlug = userRevocatingSlug,
                    Reason = reason,
                    SerialNumber = certificate.SerialNumber,
                    Thumbprint = certificate.Thumbprint
                };

                await SaveAsync(revocatedCertificate, userRevocatingSlug);
            }
        }

        protected ICertificateAuthorityService CertificateAuthority { get; }

        protected ICertificateService CertificateService { get; }

        protected NotaryConfiguration Configuration { get; }

        protected IAsymmetricKeyService KeyService { get; }
    }
}
