using System;
using System.Threading.Tasks;
using AutoMapper;
using MongoDB.Driver;
using Notary.Contract;
using Notary.Data.Model;
using Notary.Data.Repository;
using Notary.Interface.Repository;

namespace Notary.Data;

internal class AsymmetricKeyRepository : BaseRepository<AsymmetricKey, AsymmetricKeyModel>, IAsymmetricKeyRepository
{
    public AsymmetricKeyRepository(IMongoDatabase db, IMapper map) : base(db, map)
    {
    }

    public override async Task InitializeAsync()
    {
        var notAfterIndex = new CreateIndexModel<AsymmetricKeyModel>(Builders<AsymmetricKeyModel>.IndexKeys.Ascending(n => n.NotAfter));
        var notBeforeIndex = new CreateIndexModel<AsymmetricKeyModel>(Builders<AsymmetricKeyModel>.IndexKeys.Ascending(n => n.NotBefore));

        await Collection.Indexes.CreateManyAsync([notAfterIndex, notBeforeIndex]);
        await base.InitializeAsync();
    }
}
