using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Notary.Contract;

namespace Notary.Interface.Repository
{
    public interface IRepository<T>
        where T : Entity
    {
        /// <summary>
        /// Mark an entity as inactive (soft-delete)
        /// </summary>
        /// <param name="slug">The slug of the entity to delete</param>
        Task DeleteAsync(string slug, string updatedBySlug);

        /// <summary>
        /// Mark an entity as inactive (soft-delete) by the ID
        /// </summary>
        /// <param name="id">The document ID</param>
        Task DeleteByIdAsync(string id, string updatedBySlug);

        /// <summary>
        /// Get a record
        /// </summary>
        /// <param name="slug">The slug of the record</param>
        /// <returns>The record matching the slug or null if not found</returns>
        Task<T> GetAsync(string slug);

        /// <summary>
        /// Dump all documents from the collection. This is an expensive operation. Do it sparingly
        /// </summary>
        /// <returns></returns>
        Task<List<T>> GetAllAsync();

        /// <summary>
        /// Get a record by its document ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns>A record matching the ID or null if nothing found</returns>
        Task<T> GetByIdAsync(string id);

        /// <summary>
        /// Initializes a repository by setting up appropriate indexes.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Save an entity to the document store
        /// </summary>
        /// <param name="entity">The entity to update or create</param>
        Task SaveAsync(T entity);
    }
}
