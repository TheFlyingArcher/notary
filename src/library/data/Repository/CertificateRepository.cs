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
    public class CertificateRepository : BaseRepository<Certificate, CertificateModel>, ICertificateRepository
    {
        public CertificateRepository(IMongoDatabase db, IMapper map) : base(db, map)
        {
        }

        public async Task<List<Certificate>> GetCertificatesByCaAsync(string caSlug)
        {
            var filter = Builders<CertificateModel>.Filter.Eq("caSlug", caSlug);

            using (var cursor = await Collection.FindAsync(filter))
            {
                var certList = await cursor.ToListAsync();
                return certList.Select(x => Mapper.Map<Certificate>(x)).ToList();
            }
        }

        public async Task<Certificate> GetSigningCertificateAsync()
        {
            // In SQL: WHERE psc = 1 AND active = 1
            var pscFilter = Builders<CertificateModel>.Filter.Eq("psc", true);
            var activeFilter = Builders<CertificateModel>.Filter.Eq("active", true);
            var filter = Builders<CertificateModel>.Filter.And(pscFilter, activeFilter);

            //In SQL: FROM certificates
            var collection = await Collection.FindAsync(filter);

            var model = await collection.FirstOrDefaultAsync();

            if (model == null)
                return null;

            var cert = Mapper.Map<Certificate>(model);
            return cert;
        }
    }
}
