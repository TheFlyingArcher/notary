using System;
using AutoMapper;
using MongoDB.Driver;
using Notary.Contract;
using Notary.Data.Model;
using Notary.Data.Repository;
using Notary.Interface.Repository;

namespace Notary.Data;

public class AsymmetricKeyRepository : BaseRepository<AsymmetricKey, AsymmetricKeyModel>, IAsymmetricKeyRepository
{
    public AsymmetricKeyRepository(IMongoDatabase db, IMapper map) : base(db, map)
    {
    }
}
