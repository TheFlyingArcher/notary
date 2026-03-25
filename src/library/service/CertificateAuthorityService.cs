using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using log4net;
using Notary.Configuration;
using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;

namespace Notary.Service;

public class CertificateAuthorityService : CryptographicEntityService<CertificateAuthority>,
    ICertificateAuthorityService
{
    public CertificateAuthorityService(
        ICertificateAuthorityRepository repo,
        ICertificateService caService,
        NotaryConfiguration config, ILog log) : base(repo, log)
    {
        CertificateService = caService;
        Configuration = config;
    }

    protected ICertificateService CertificateService { get; }

    protected NotaryConfiguration Configuration { get; }

    public override async Task SaveAsync(CertificateAuthority entity, string updatedBySlug)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (entity.KeyAlgorithm == Algorithm.RSA && !entity.KeyLength.HasValue)
            throw new ArgumentException("Must supply a key length if using RSA");

        CertificateAuthority parentCa = null;
        if (!string.IsNullOrEmpty(entity.ParentCaSlug))
        {
            parentCa = await Repository.GetAsync(entity.ParentCaSlug);
            if (parentCa == null)
                throw new ArgumentNullException(nameof(parentCa));

            entity.KeyCurve = parentCa.KeyCurve;
            entity.KeyAlgorithm = parentCa.KeyAlgorithm;
            entity.KeyLength = parentCa.KeyLength;
            entity.ParentCaSlug = parentCa.Slug;
        }

        var caRequest = new CertificateRequest
        {
            CertificateKeyUsageFlags = new List<int>
            {
                (int)CertificateKeyUsage.CrlSign,
                (int)CertificateKeyUsage.KeyCertSign,
                (int)CertificateKeyUsage.DigitalSignature,
                (int)CertificateKeyUsage.KeyEncipherment,
                (int)CertificateKeyUsage.DataEncipherment
            },
            CrlEndpoint = entity.CrlEndpoint,
            Curve = entity.KeyCurve,
            IsCaCertificate = true,
            KeyAlgorithm = entity.KeyAlgorithm,
            KeySize = entity.KeyLength,
            ExtendedKeyUsages = new List<string>
            {
                "1.3.6.1.5.5.7.3.3", // Code signing
                "1.3.6.1.5.5.7.3.1", // Server Authentication
                "1.3.6.1.5.5.7.3.2", // Client Authentication
                "1.3.6.1.4.1.311.20.2.2" //SMART Card Login
            },
            Name = entity.Name,
            NotAfter = entity.NotAfter,
            NotBefore = entity.NotBefore,
            ParentCertificateSlug = parentCa != null ? parentCa.CertificateSlug : null,
            RequestedBySlug = entity.CreatedBySlug,
            Subject = entity.DistinguishedName
        };

        var caCertificate = await CertificateService.IssueCertificateAsync(caRequest);
        entity.IssuingDn = parentCa != null ? parentCa.DistinguishedName : null;
        entity.CertificateSlug = caCertificate.Slug;

        await base.SaveAsync(entity, updatedBySlug);
    }

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
}