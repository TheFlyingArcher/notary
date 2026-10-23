using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using log4net;

using Notary.Configuration;
using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;
using Notary.Security;
using Notary.Profiles;

namespace Notary.Service
{
    public class CertificateAuthorityService : CryptographicEntityService<CertificateAuthority>, ICertificateAuthorityService
    {
        public CertificateAuthorityService(
            ICertificateAuthorityRepository repo,
            ICertificateService caService,
            NotaryConfiguration config, ILog log) : base(repo, log)
        {
            CertificateService = caService;
            Configuration = config;
        }

        [NotaryAuthorize(NotaryOperation.CertificateAuthorityList)]
        public override Task<List<CertificateAuthority>> GetAllAsync() => base.GetAllAsync();

        [NotaryAuthorize(NotaryOperation.CertificateAuthorityGet)]
        public override Task<CertificateAuthority> GetAsync(string slug) => base.GetAsync(slug);

        [NotaryAuthorize(NotaryOperation.CertificateAuthorityRevoke)]
        public override Task DeleteAsync(string slug, string updatedBySlug) => base.DeleteAsync(slug, updatedBySlug);

        [NotaryAuthorize(NotaryOperation.CertificateAuthorityCreate)]
        public override async Task SaveAsync(CertificateAuthority entity, string updatedBySlug)
        {
            var result = await CreateAsync(entity, updatedBySlug);
            if (!result.IsSuccess)
                throw new NotaryException(result.Error);
        }

        [NotaryAuthorize(NotaryOperation.CertificateAuthorityCreate)]
        public async Task<Result<CertificateAuthority>> CreateAsync(CertificateAuthority entity, string updatedBySlug)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (entity.KeyAlgorithm == Algorithm.RSA && !entity.KeyLength.HasValue)
                throw new ArgumentException("Must supply a key length if using RSA");

            var nameCheck = entity.DistinguishedName.Check();
            if (!nameCheck.IsSuccess)
                return Result<CertificateAuthority>.Fail(nameCheck.Status, nameCheck.Error);

            CertificateAuthority parentCa = null;
            if (!string.IsNullOrEmpty(entity.ParentCaSlug))
            {
                parentCa = await Repository.GetAsync(entity.ParentCaSlug);
                if (parentCa == null)
                    throw new ArgumentNullException(nameof(parentCa));

                var issuerCheck = CaProfileCatalog.CheckIssuer(parentCa, CaProfileCatalog.Resolve(entity));
                if (!issuerCheck.IsSuccess)
                    return Result<CertificateAuthority>.Fail(issuerCheck.Status, issuerCheck.Error);

                entity.KeyCurve = parentCa.KeyCurve;
                entity.KeyAlgorithm = parentCa.KeyAlgorithm;
                entity.KeyLength = parentCa.KeyLength;
                entity.ParentCaSlug = parentCa.Slug;
            }

            // CAs created without a purpose keep the broad usages every CA had before purposes existed
            bool legacyUsages = UsesLegacyUsages(entity);
            var profile = CaProfileCatalog.Resolve(entity);

            var caRequest = new CertificateRequest
            {
                CertificateKeyUsageFlags = legacyUsages
                    ? new List<int>
                    {
                        (int)CertificateKeyUsage.CrlSign,
                        (int)CertificateKeyUsage.KeyCertSign,
                        (int)CertificateKeyUsage.DigitalSignature,
                        (int)CertificateKeyUsage.KeyEncipherment,
                        (int)CertificateKeyUsage.DataEncipherment
                    }
                    : [.. profile.CaKeyUsages.Select(u => (int)u)],
                Curve = entity.KeyCurve,
                IsCaCertificate = true,
                KeyAlgorithm = entity.KeyAlgorithm,
                KeySize = entity.KeyLength,
                ExtendedKeyUsages = legacyUsages
                    ? new List<string>
                    {
                        "1.3.6.1.5.5.7.3.3", // Code signing
                        "1.3.6.1.5.5.7.3.1", // Server Authentication
                        "1.3.6.1.5.5.7.3.2", // Client Authentication
                        "1.3.6.1.4.1.311.20.2.2" //SMART Card Login
                    }
                    : [.. profile.CaExtendedKeyUsages],
                Name = entity.Name,
                NotAfter = entity.NotAfter,
                NotBefore = entity.NotBefore,
                ParentCertificateSlug = parentCa != null ? parentCa.CertificateSlug : null,
                PathLengthConstraint = CaProfileCatalog.GetPathLength(entity.Purpose, parentCa != null),
                RequestedBySlug = entity.CreatedBySlug,
                Subject = entity.DistinguishedName
            };

            var issued = await CertificateService.IssueCertificateAsync(caRequest);
            if (!issued.IsSuccess)
                return Result<CertificateAuthority>.Fail(issued.Status, issued.Error);

            entity.IssuingDn = parentCa != null ? parentCa.DistinguishedName : null;
            entity.CertificateSlug = issued.Value.Slug;

            await base.SaveAsync(entity, updatedBySlug);

            return Result<CertificateAuthority>.Ok(entity);
        }

        private static bool UsesLegacyUsages(CertificateAuthority ca)
        {
            bool hasCustomUsages = ca.CustomKeyUsages is { Count: > 0 } || ca.CustomExtendedKeyUsages is { Count: > 0 };

            return ca.Purpose is null || (ca.Purpose is CaPurpose.Custom && !hasCustomUsages);
        }

        [NotaryAuthorize(NotaryOperation.CertificateAuthorityList)]
        public async Task<List<CaBrief>> GetCaListBrief()
        {
            var caList = await ((ICertificateAuthorityRepository)Repository).GetAllAsync();
            var caListBrief = new List<CaBrief>();

            foreach (var ca in caList)
            {
                var brief = new CaBrief
                {
                    Name = ca.Name,
                    Slug = ca.Slug,
                    CreatedOn = ca.Created.ToString()
                };

                if (!string.IsNullOrEmpty(ca.ParentCaSlug))
                {
                    var parentCa = await GetAsync(ca.ParentCaSlug);
                    brief.ParentName = parentCa.Name;
                }

                //brief.CreatedBy = createdBy.Name;
                caListBrief.Add(brief);
            }

            return caListBrief;
        }

        [NotaryAuthorize(NotaryOperation.CertificateAuthorityList)]
        public async Task<List<CertificateAuthority>> GetLeafIssuingAuthoritiesAsync()
        {
            var issuers = new List<CertificateAuthority>();

            foreach (var ca in await Repository.GetAllAsync())
            {
                if (!ca.IsIssuer || !CaProfileCatalog.Resolve(ca).IssuesLeafCertificates)
                    continue;

                // A CA whose certificate is revoked (or missing) can no longer issue
                var certificate = await CertificateService.GetAsync(ca.CertificateSlug);
                if (certificate == null || certificate.RevocationDate.HasValue)
                    continue;

                issuers.Add(ca);
            }

            return [.. issuers.OrderBy(ca => ca.Name, StringComparer.OrdinalIgnoreCase)];
        }

        protected ICertificateService CertificateService { get; }

        protected NotaryConfiguration Configuration { get; }
    }
}
