using System;
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
    }
}
