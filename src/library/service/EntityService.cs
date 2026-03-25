using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using log4net;
using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;

namespace Notary.Service;

public abstract class EntityService<TC> : IEntityService<TC>
    where TC : Entity
{
    public EntityService(IRepository<TC> repository, ILog log)
    {
        Repository = repository;
        Logger = log;
    }

    public IRepository<TC> Repository { get; }

    public ILog Logger { get; }

    public virtual async Task DeleteAsync(string slug, string updatedBySlug)
    {
        await Repository.DeleteAsync(slug, updatedBySlug);
    }

    public virtual async Task<List<TC>> GetAllAsync()
    {
        return await Repository.GetAllAsync();
    }

    public virtual async Task<TC> GetAsync(string slug)
    {
        return await Repository.GetAsync(slug);
    }

    public virtual async Task SaveAsync(TC entity, string updatedBySlug)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await Repository.SaveAsync(entity);
    }

    public virtual async Task InitializeAsync()
    {
        await Repository.InitializeAsync();
    }
}