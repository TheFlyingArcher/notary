using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Notary.Configuration;
using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;

namespace Notary.Service
{
    /// <summary>
    /// Revokes certificates and publishes RFC 5280 CRLs for the certificate authorities that issued them.
    /// </summary>
    internal class CertificateRevokeService : CryptographicEntityService<RevocatedCertificate>, ICertificateRevokeService
    {
        // CRL issuance reads, increments and writes the per-CA CRL number. Serialize it within the process.
        private static readonly SemaphoreSlim s_crlLock = new(1, 1);

        // The bit position of cRLSign in the X.509 KeyUsage extension
        private const int CrlSignKeyUsageBit = 6;

        private readonly IRevocatedCertificateRepository _revocationRepository;
        private readonly ICrlRecordRepository _crlRepository;

        public CertificateRevokeService(
            IRevocatedCertificateRepository revocatedCertificateRepo,
            ICrlRecordRepository crlRecordRepo,
            ICertificateService certificateService,
            IAsymmetricKeyService keyService,
            ILog log,
            NotaryConfiguration config
        ) : base(revocatedCertificateRepo, log)
        {
            _revocationRepository = revocatedCertificateRepo;
            _crlRepository = crlRecordRepo;
            CertificateService = certificateService;
            Configuration = config;
            KeyService = keyService;
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await BackfillLegacyRevocationsAsync();
        }

        public async Task<Result<CrlDocument>> GetCrlAsync(string caSlug, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(caSlug))
                return Result<CrlDocument>.Fail(ResultStatus.NotFound, "CA not found");

            await s_crlLock.WaitAsync(cancellationToken);
            try
            {
                var existing = await _crlRepository.GetAsync(CrlRecord.SlugFor(caSlug));
                if (existing is { Active: true } && DateTime.UtcNow < GetRefreshTime(existing))
                    return Result<CrlDocument>.Ok(ToDocument(existing));

                return await IssueCrlAsync(caSlug, existing, cancellationToken);
            }
            finally
            {
                s_crlLock.Release();
            }
        }

        public async Task<List<RevocatedCertificate>> GetRevocatedCertificates()
        {
            var revocatedCerts = await Repository.GetAllAsync();

            return revocatedCerts.Where(r => r.Active).ToList();
        }

        public async Task<RevocatedCertificate> GetRevocationAsync(string certificateSlug)
        {
            return await _revocationRepository.GetActiveByCertificateAsync(certificateSlug);
        }

        public async Task<Result<int>> RevokeCertificateAsync(
            string slug,
            RevocationReason reason,
            string userRevocatingSlug,
            DateTime? invalidityDate = null,
            CancellationToken cancellationToken = default)
        {
            if (!Enum.IsDefined(reason) || reason == RevocationReason.RemoveFromCrl)
                return Result<int>.Fail(ResultStatus.Invalid, $"{reason} is not a valid reason for revoking a certificate");

            var certificate = await CertificateService.GetAsync(slug);
            if (certificate == null)
                return Result<int>.Fail(ResultStatus.NotFound, "Certificate not found");

            if (certificate.RevocationDate.HasValue)
                return Result<int>.Fail(ResultStatus.Conflict, "Certificate is already revoked");

            var now = TruncateToSeconds(DateTime.UtcNow);
            var invalidity = invalidityDate ?? (IsCompromise(reason) ? now : null);
            if (invalidity > now)
                return Result<int>.Fail(ResultStatus.Invalid, "The invalidity date cannot be in the future");

            // A CA on hold is temporary, so what it issued is not condemned. Any other CA revocation takes the whole subtree down.
            var targets = new List<(Certificate Certificate, RevocationReason Reason, DateTime? Invalidity)>
            {
                (certificate, reason, invalidity)
            };
            if (certificate.IsCaCertificate && reason != RevocationReason.CertificateHold)
            {
                var issued = new List<Certificate>();
                await CollectIssuedAsync(certificate.Slug, [], issued);
                targets.AddRange(issued.Select(c => (c, RevocationReason.CaCompromized, (DateTime?)now)));
            }

            var revoked = 0;
            var failed = new List<string>();
            var issuers = new HashSet<string>();
            foreach (var (target, targetReason, targetInvalidity) in targets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await RevokeOneAsync(target, targetReason, userRevocatingSlug, now, targetInvalidity);
                    revoked++;
                    if (!string.IsNullOrEmpty(target.IssuingSlug))
                        issuers.Add(target.IssuingSlug);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to revoke certificate {target.Slug}", ex);
                    failed.Add(target.Slug);
                }
            }

            await RefreshCrlsAsync(issuers, cancellationToken);

            return failed.Count == 0
                ? Result<int>.Ok(revoked)
                : Result<int>.Fail(ResultStatus.Failed, $"{revoked} certificate(s) revoked; failed to revoke: {string.Join(", ", failed)}");
        }

        public async Task<Result> ReinstateCertificateAsync(string slug, string userSlug, CancellationToken cancellationToken = default)
        {
            var certificate = await CertificateService.GetAsync(slug);
            if (certificate == null)
                return Result.Fail(ResultStatus.NotFound, "Certificate not found");

            var revocation = await _revocationRepository.GetActiveByCertificateAsync(slug);
            if (revocation == null)
                return Result.Fail(ResultStatus.Conflict, "Certificate is not revoked");

            if (revocation.Reason != RevocationReason.CertificateHold)
                return Result.Fail(ResultStatus.Invalid, "Only certificates on hold can be reinstated");

            revocation.Active = false;
            revocation.Updated = DateTime.UtcNow;
            revocation.UpdatedBySlug = userSlug;
            await _revocationRepository.SaveAsync(revocation);

            certificate.RevocationDate = null;
            await CertificateService.SaveAsync(certificate, userSlug);

            if (!string.IsNullOrEmpty(revocation.IssuerSlug))
                await RefreshCrlsAsync([revocation.IssuerSlug], cancellationToken);

            return Result.Ok();
        }

        private async Task RevokeOneAsync(Certificate certificate, RevocationReason reason, string userSlug, DateTime now, DateTime? invalidityDate)
        {
            var revocation = new RevocatedCertificate
            {
                Active = true,
                CertificateSlug = certificate.Slug,
                Created = now,
                CreatedBySlug = userSlug,
                InvalidityDate = invalidityDate,
                IssuerSlug = string.IsNullOrEmpty(certificate.IssuingSlug) ? null : certificate.IssuingSlug,
                Reason = reason,
                RevocationDate = now,
                SerialNumber = certificate.SerialNumber,
                Thumbprint = certificate.Thumbprint
            };

            // Record the revocation first so a failure never leaves a certificate stamped revoked but missing from the CRL
            await _revocationRepository.SaveAsync(revocation);
            try
            {
                certificate.RevocationDate = now;
                await CertificateService.SaveAsync(certificate, userSlug);
            }
            catch
            {
                certificate.RevocationDate = null;
                revocation.Active = false;
                await _revocationRepository.SaveAsync(revocation);
                throw;
            }
        }

        private async Task CollectIssuedAsync(string caSlug, HashSet<string> visited, List<Certificate> issued)
        {
            if (!visited.Add(caSlug))
                return;

            foreach (var child in await CertificateService.GetCertificatesByCaAsync(caSlug))
            {
                if (!child.RevocationDate.HasValue)
                    issued.Add(child);

                if (child.IsCaCertificate)
                    await CollectIssuedAsync(child.Slug, visited, issued);
            }
        }

        /// <summary>
        /// Reissue the CRLs of the given CAs. A failure is logged and the cached CRL invalidated so the next
        /// request rebuilds it; the revocation itself has already been recorded.
        /// </summary>
        private async Task RefreshCrlsAsync(IEnumerable<string> caSlugs, CancellationToken cancellationToken)
        {
            foreach (var caSlug in caSlugs)
            {
                try
                {
                    await s_crlLock.WaitAsync(cancellationToken);
                    try
                    {
                        var existing = await _crlRepository.GetAsync(CrlRecord.SlugFor(caSlug));
                        var result = await IssueCrlAsync(caSlug, existing, cancellationToken);
                        if (!result.IsSuccess)
                            Logger.Warn($"CRL for {caSlug} was not reissued: {result.Error}");
                    }
                    finally
                    {
                        s_crlLock.Release();
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Logger.Error($"Failed to reissue the CRL for {caSlug}", ex);
                    await InvalidateCrlAsync(caSlug);
                }
            }
        }

        private async Task InvalidateCrlAsync(string caSlug)
        {
            try
            {
                var existing = await _crlRepository.GetAsync(CrlRecord.SlugFor(caSlug));
                if (existing != null)
                {
                    existing.Active = false;
                    await _crlRepository.SaveAsync(existing);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to invalidate the cached CRL for {caSlug}", ex);
            }
        }

        /// <summary>
        /// Build, sign, verify and persist a new CRL. The caller must hold <see cref="s_crlLock"/>.
        /// </summary>
        private async Task<Result<CrlDocument>> IssueCrlAsync(string caSlug, CrlRecord existing, CancellationToken cancellationToken)
        {
            var caCert = await CertificateService.GetAsync(caSlug);
            if (caCert == null || !caCert.IsCaCertificate)
                return Result<CrlDocument>.Fail(ResultStatus.NotFound, "CA not found");

            var signingCertificate = GetX509FromPem(caCert.Data);
            var now = TruncateToSeconds(DateTime.UtcNow);
            if (now > signingCertificate.NotAfter)
                return Result<CrlDocument>.Fail(ResultStatus.Invalid, "The CA certificate has expired");

            var keyUsage = signingCertificate.GetKeyUsage();
            if (keyUsage != null && !keyUsage[CrlSignKeyUsageBit])
                return Result<CrlDocument>.Fail(ResultStatus.Invalid, "The CA certificate is not permitted to sign CRLs");

            cancellationToken.ThrowIfCancellationRequested();

            var keyInfo = await KeyService.GetAsync(caCert.KeySlug);
            var keyPair = await KeyService.GetKeyPairAsync(caCert.KeySlug);
            if (keyInfo == null || keyPair == null)
                return Result<CrlDocument>.Fail(ResultStatus.NotFound, "The CA signing key was not found");

            var revocations = await _revocationRepository.GetActiveByIssuerAsync(caSlug);

            var validityDays = Configuration.CrlValidityDays > 0 ? Configuration.CrlValidityDays : 7;
            var nextUpdate = now.AddDays(validityDays);

            var crlGen = new X509V2CrlGenerator();
            crlGen.SetIssuerDN(signingCertificate.SubjectDN);
            crlGen.SetThisUpdate(now);
            crlGen.SetNextUpdate(nextUpdate);

            var entries = revocations
                .GroupBy(r => r.SerialNumber, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderBy(r => r.RevocationDate).First())
                .ToList();
            foreach (var revocation in entries)
            {
                var entryExtensions = new X509ExtensionsGenerator();
                // RFC 5280 5.3.1: reasonCode "unspecified" SHOULD be omitted
                if (revocation.Reason != RevocationReason.Unspecified)
                    entryExtensions.AddExtension(X509Extensions.ReasonCode, false, new CrlReason(MapRevocationReason(revocation.Reason)));
                if (revocation.InvalidityDate.HasValue)
                    entryExtensions.AddExtension(X509Extensions.InvalidityDate, false, new Asn1GeneralizedTime(TruncateToSeconds(revocation.InvalidityDate.Value)));

                var revokedOn = revocation.RevocationDate == default ? revocation.Created : revocation.RevocationDate;
                crlGen.AddCrlEntry(
                    new BigInteger(revocation.SerialNumber, 16),
                    TruncateToSeconds(revokedOn),
                    entryExtensions.IsEmpty ? null : entryExtensions.Generate());
            }

            // Continue from the persisted counter. A first CRL starts at the Unix time so it also exceeds any number
            // handed out by the previous time-based implementation.
            var crlNumber = existing != null
                ? existing.Number + 1
                : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            crlGen.AddExtension(X509Extensions.CrlNumber, false, new CrlNumber(BigInteger.ValueOf(crlNumber)));
            crlGen.AddExtension(X509Extensions.AuthorityKeyIdentifier, false, new AuthorityKeyIdentifierStructure(signingCertificate));

            var signatureAlgorithm = keyInfo.KeyAlgorithm == Algorithm.EllipticCurve
                ? "SHA256WithECDSA"
                : "SHA256WithRSA";
            var crl = crlGen.Generate(new Asn1SignatureFactory(signatureAlgorithm, keyPair.Private));
            crl.Verify(signingCertificate.GetPublicKey());
            var encoded = crl.GetEncoded();

            var record = existing ?? new CrlRecord
            {
                CaSlug = caSlug,
                Created = now,
                CreatedBySlug = string.Empty
            };
            record.Slug = CrlRecord.SlugFor(caSlug);
            record.Active = true;
            record.Updated = now;
            record.Number = crlNumber;
            record.ThisUpdate = now;
            record.NextUpdate = nextUpdate;
            record.EntryCount = entries.Count;
            record.Data = Convert.ToBase64String(encoded);
            await _crlRepository.SaveAsync(record);

            return Result<CrlDocument>.Ok(ToDocument(record));
        }

        /// <summary>
        /// Revocations written before the issuer and revocation date were recorded cannot be placed in a CRL.
        /// Fill them in from the certificate.
        /// </summary>
        private async Task BackfillLegacyRevocationsAsync()
        {
            foreach (var revocation in await _revocationRepository.GetWithoutIssuerAsync())
            {
                var certificate = await CertificateService.GetAsync(revocation.CertificateSlug);
                if (certificate == null || string.IsNullOrEmpty(certificate.IssuingSlug))
                    continue;

                revocation.IssuerSlug = certificate.IssuingSlug;
                if (revocation.RevocationDate == default)
                    revocation.RevocationDate = DateTime.SpecifyKind(revocation.Created, DateTimeKind.Utc);

                await _revocationRepository.SaveAsync(revocation);
            }
        }

        // A CRL is refreshed once half of its validity has elapsed, so clients always find one that is current
        private static DateTime GetRefreshTime(CrlRecord record) =>
            record.ThisUpdate + (record.NextUpdate - record.ThisUpdate) / 2;

        private static CrlDocument ToDocument(CrlRecord record) =>
            new(Convert.FromBase64String(record.Data), record.Number, record.ThisUpdate, record.NextUpdate);

        private static bool IsCompromise(RevocationReason reason) =>
            reason is RevocationReason.KeyCompromized or RevocationReason.CaCompromized or RevocationReason.AaCompromized;

        // ASN.1 times used in CRLs have one second resolution
        private static DateTime TruncateToSeconds(DateTime value)
        {
            var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
            return new DateTime(utc.Ticks - utc.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        }

        private static int MapRevocationReason(RevocationReason reason)
        {
            return reason switch
            {
                RevocationReason.Unspecified          => CrlReason.Unspecified,
                RevocationReason.KeyCompromized       => CrlReason.KeyCompromise,
                RevocationReason.CaCompromized        => CrlReason.CACompromise,
                RevocationReason.AffiliationChanged   => CrlReason.AffiliationChanged,
                RevocationReason.Superceded           => CrlReason.Superseded,
                RevocationReason.CessationOfOperation => CrlReason.CessationOfOperation,
                RevocationReason.CertificateHold      => CrlReason.CertificateHold,
                RevocationReason.RemoveFromCrl        => CrlReason.RemoveFromCrl,
                RevocationReason.PrivilegeWithdrawn   => CrlReason.PrivilegeWithdrawn,
                RevocationReason.AaCompromized        => CrlReason.AACompromise,
                _                                     => CrlReason.Unspecified
            };
        }

        protected ICertificateService CertificateService { get; }

        protected NotaryConfiguration Configuration { get; }

        protected IAsymmetricKeyService KeyService { get; }
    }
}
