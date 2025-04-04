using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MongoDB.Driver;
using Notary.Contract;
using Notary.Data.Model;
using Notary.Interface.Repository;

namespace Notary.Data.Repository
{
    internal class CertificateRepository : BaseRepository<Certificate, CertificateModel>, ICertificateRepository
    {
        public CertificateRepository(IMongoDatabase db, IMapper map) : base(db, map)
        {
        }

        public override async Task InitializeAsync()
        {
            var issuingSlugIndex = new CreateIndexModel<CertificateModel>(Builders<CertificateModel>.IndexKeys.Ascending(i => i.IssuingSlug));
            var keySlugIndex = new CreateIndexModel<CertificateModel>(Builders<CertificateModel>.IndexKeys.Ascending(k => k.KeySlug));
            var notAfterIndex = new CreateIndexModel<CertificateModel>(Builders<CertificateModel>.IndexKeys.Ascending(n => n.NotAfter));
            var notBeforeIndex = new CreateIndexModel<CertificateModel>(Builders<CertificateModel>.IndexKeys.Ascending(n => n.NotBefore));

            await Collection.Indexes.CreateManyAsync([
                issuingSlugIndex,
                keySlugIndex,
                notAfterIndex,
                notBeforeIndex
            ]);

            await base.InitializeAsync();
        }

        public async Task<List<Certificate>> GetCertificatesByCaAsync(string caSlug)
        {
            var filter = Builders<CertificateModel>.Filter.Eq("iss_slug", caSlug);

            using (var cursor = await Collection.FindAsync(filter))
            {
                var certList = await cursor.ToListAsync();
                return certList.Select(x => Mapper.Map<Certificate>(x)).ToList();
            }
        }
    }
}
