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
    internal class RevocatedCertificateRepository : BaseRepository<RevocatedCertificate, RevocatedCertificateModel>, IRevocatedCertificateRepository
    {
        public RevocatedCertificateRepository(IMongoDatabase db, IMapper map) : base(db, map)
        {
        }

        public override async Task InitializeAsync()
        {
            var issuerIndex = new CreateIndexModel<RevocatedCertificateModel>(Builders<RevocatedCertificateModel>.IndexKeys.Ascending(r => r.IssuerSlug));
            var certificateIndex = new CreateIndexModel<RevocatedCertificateModel>(Builders<RevocatedCertificateModel>.IndexKeys.Ascending(r => r.CertificateSlug));

            await Collection.Indexes.CreateManyAsync([issuerIndex, certificateIndex]);
            await base.InitializeAsync();
        }

        public async Task<List<RevocatedCertificate>> GetActiveByIssuerAsync(string issuerSlug)
        {
            var filter = Builders<RevocatedCertificateModel>.Filter.Where(r => r.IssuerSlug == issuerSlug && r.Active);
            return await FindAsync(filter);
        }

        public async Task<RevocatedCertificate> GetActiveByCertificateAsync(string certificateSlug)
        {
            var filter = Builders<RevocatedCertificateModel>.Filter.Where(r => r.CertificateSlug == certificateSlug && r.Active);
            return await RunQuery(filter);
        }

        private async Task<List<RevocatedCertificate>> FindAsync(FilterDefinition<RevocatedCertificateModel> filter)
        {
            using var cursor = await Collection.FindAsync(filter);
            var models = await cursor.ToListAsync();
            return models.Select(Mapper.Map<RevocatedCertificate>).ToList();
        }
    }
}
