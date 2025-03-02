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

            var keyPair = await KeyService.GetKeyPairAsync(caCert.KeySlug);
            var signingCertificate = GetX509FromPem(caCert.Data);
            var crlGen = new X509V2CrlGenerator();

            var revocatedCerts = await GetRevocatedCertificates();

            crlGen.SetIssuerDN(signingCertificate.SubjectDN);
            crlGen.SetThisUpdate(DateTime.UtcNow);
            crlGen.SetNextUpdate(DateTime.UtcNow.AddYears(1));

            foreach (var cert in revocatedCerts)
            {
                var rc = await CertificateService.GetAsync(cert.CertificateSlug);
                var certificate = GetX509FromPem(rc.Data);

                int reason = -1;
                switch (cert.Reason)
                {
                    case RevocationReason.Unspecified:
                        reason = CrlReason.Unspecified;
                        break;
                    case RevocationReason.KeyCompromized:
                        reason = CrlReason.KeyCompromise;
                        break;
                    case RevocationReason.CaCompromized:
                        reason = CrlReason.CACompromise;
                        break;
                    case RevocationReason.AffiliationChanged:
                        reason = CrlReason.AffiliationChanged;
                        break;
                    case RevocationReason.Superceded:
                        reason = CrlReason.Superseded;
                        break;
                    case RevocationReason.CessationOfOperation:
                        reason = CrlReason.CessationOfOperation;
                        break;
                    case RevocationReason.CertificateHold:
                        reason = CrlReason.CertificateHold;
                        break;
                    case RevocationReason.RemoveFromCrl:
                        reason = CrlReason.RemoveFromCrl;
                        break;
                    case RevocationReason.PrivilegeWithdrawn:
                        reason = CrlReason.PrivilegeWithdrawn;
                        break;
                    case RevocationReason.AaCompromized:
                        reason = CrlReason.AACompromise;
                        break;
                }

                crlGen.AddCrlEntry(certificate.SerialNumber, cert.Created, reason);
                crlGen.AddExtension(X509Extensions.CrlNumber, false, new CrlNumber(BigInteger.Arbitrary(16)));
            }

            var signatureFactory = new Asn1SignatureFactory("SHA256WithRSA", keyPair.Private);
            crlGen.AddExtension(X509Extensions.AuthorityKeyIdentifier, false, new AuthorityKeyIdentifierStructure(signingCertificate));
            var crl = crlGen.Generate(signatureFactory);

            return crl.GetEncoded();
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
