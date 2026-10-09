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
    internal class RevocationRepository : BaseRepository<Revocation, RevocationModel>, IRevocationRepository
    {
        public RevocationRepository(IMongoDatabase db, IMapper map) : base(db, map)
        {
        }

        public override async Task InitializeAsync()
        {
            var issuerIndex = new CreateIndexModel<RevocationModel>(Builders<RevocationModel>.IndexKeys.Ascending(r => r.IssuerSlug));
            var certificateIndex = new CreateIndexModel<RevocationModel>(Builders<RevocationModel>.IndexKeys.Ascending(r => r.CertificateSlug));

            await Collection.Indexes.CreateManyAsync([issuerIndex, certificateIndex]);
            await base.InitializeAsync();
        }

        public async Task<List<Revocation>> GetActiveByIssuerAsync(string issuerSlug)
        {
            var filter = Builders<RevocationModel>.Filter.Where(r => r.IssuerSlug == issuerSlug && r.Active);
            return await FindAsync(filter);
        }

        public async Task<Revocation> GetActiveByCertificateAsync(string certificateSlug)
        {
            var filter = Builders<RevocationModel>.Filter.Where(r => r.CertificateSlug == certificateSlug && r.Active);
            return await RunQuery(filter);
        }

        private async Task<List<Revocation>> FindAsync(FilterDefinition<RevocationModel> filter)
        {
            using var cursor = await Collection.FindAsync(filter);
            var models = await cursor.ToListAsync();
            return models.Select(Mapper.Map<Revocation>).ToList();
        }
    }
}
