using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Notary.Contract;
using Notary.Interface.Repository;
using Notary.Interface.Service;

namespace Notary.Service
{
    public abstract class EntityService<TC> : IEntityService<TC>
        where TC : Entity
    {
        public IRepository<TC> Repository { get; private set; }

        public ILog Logger { get; }

        public EntityService(IRepository<TC> repository, ILog log)
        {
            Repository = repository;
            Logger = log;
        }

        public async virtual Task DeleteAsync(string slug, string updatedBySlug)
        {
            await Repository.DeleteAsync(slug, updatedBySlug);
        }

        public async virtual Task<List<TC>> GetAllAsync()
        {
            return await Repository.GetAllAsync();
        }

        public async virtual Task<TC> GetAsync(string slug)
        {
            return await Repository.GetAsync(slug);
        }

        public async virtual Task SaveAsync(TC entity, string updatedBySlug)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await Repository.SaveAsync(entity);
        }

        public async virtual Task InitializeAsync()
        {
            await Repository.InitializeAsync();
        }
    }
}
