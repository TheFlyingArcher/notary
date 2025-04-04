using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using MongoDB.Driver;
using Notary.Contract;
using Notary.Data.Model;
using Notary.Interface.Repository;

namespace Notary.Data.Repository
{
    internal class CertificateAuthorityRepository : BaseRepository<CertificateAuthority, CertificateAuthorityModel>, ICertificateAuthorityRepository
    {
        public CertificateAuthorityRepository(IMongoDatabase db, IMapper map) : base(db, map)
        {
        }

        public override async Task InitializeAsync()
        {
            var certSlugIndex = new CreateIndexModel<CertificateAuthorityModel>(Builders<CertificateAuthorityModel>.IndexKeys.Ascending(c => c.CertificateSlug));
            var parentSlugIndex = new CreateIndexModel<CertificateAuthorityModel>(Builders<CertificateAuthorityModel>.IndexKeys.Ascending(p => p.ParentCaSlug));
            var notBeforeIndex = new CreateIndexModel<CertificateAuthorityModel>(Builders<CertificateAuthorityModel>.IndexKeys.Ascending(n => n.NotBefore));
            var notAfterIndex = new CreateIndexModel<CertificateAuthorityModel>(Builders<CertificateAuthorityModel>.IndexKeys.Ascending(n => n.NotAfter));
            var nameIndex = new CreateIndexModel<CertificateAuthorityModel>(Builders<CertificateAuthorityModel>.IndexKeys.Ascending(n => n.Name));

            await Collection.Indexes.CreateManyAsync([
                certSlugIndex,
                parentSlugIndex,
                notAfterIndex,
                notBeforeIndex,
                nameIndex
            ]);

            await base.InitializeAsync();
        }
    }
}
