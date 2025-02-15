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
    }
}
