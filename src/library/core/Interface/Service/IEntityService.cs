using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Notary.Contract;

namespace Notary.Interface.Service
{
    /// <summary>
    /// Defines basic implementations of a service
    /// </summary>
    /// <typeparam name="TEntity">The type of entity with which to interface</typeparam>
    public interface IEntityService<TEntity> where TEntity : Entity
    {
        /// <summary>
        /// Mark an entity as having been deleted.
        /// </summary>
        /// <param name="slug">The entity slug to delete</param>
        /// <param name="updatedBySlug">The account that deleted the record</param>
        Task DeleteAsync(string slug, string updatedBySlug);

        /// <summary>
        /// Get every document from the collection. Use sparingly as this can be an expensive call.
        /// </summary>
        /// <returns>Returns a list of the entire collection</returns>
        Task<List<TEntity>> GetAllAsync();

        /// <summary>
        /// Get a single record
        /// </summary>
        /// <param name="slug">The matching slug of the document</param>
        /// <returns>A record matching the slug or null if not found</returns>
        Task<TEntity> GetAsync(string slug);

        /// <summary>
        /// Persist a record to the data store.
        /// </summary>
        /// <param name="entity">The entity to persist to the data store</param>
        /// <param name="updatedBySlug">The account that persisted the entity</param>
        Task SaveAsync(TEntity entity, string updatedBySlug);

        /// <summary>
        /// Initialize the underlying datastore
        /// </summary>
        Task InitializeAsync();
    }
}
