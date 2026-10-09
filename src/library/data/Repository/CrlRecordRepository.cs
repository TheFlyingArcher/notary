using AutoMapper;
using MongoDB.Driver;
using Notary.Contract;
using Notary.Data.Model;
using Notary.Interface.Repository;

namespace Notary.Data.Repository
{
    internal class CrlRecordRepository : BaseRepository<CrlRecord, CrlRecordModel>, ICrlRecordRepository
    {
        public CrlRecordRepository(IMongoDatabase db, IMapper map) : base(db, map)
        {
        }
    }
}
