using Supabase.Postgrest.Models;

namespace SongAcronymBot.Domain.Repositories
{
    /// <summary>
    /// Generic repository interface for CRUD operations on Supabase tables
    /// </summary>
    /// <typeparam name="T">The entity type that inherits from BaseModel</typeparam>
    public interface IBaseRepository<T> where T : BaseModel, new()
    {
        /// <summary>
        /// Gets all entities of type T
        /// </summary>
        /// <returns>A collection of all entities</returns>
        Task<List<T>> GetAllAsync();

        /// <summary>
        /// Gets an entity by its primary key
        /// </summary>
        /// <param name="id">The primary key value</param>
        /// <returns>The entity if found, otherwise null</returns>
        Task<T?> GetByIdAsync(object id);

        /// <summary>
        /// Inserts a new entity
        /// </summary>
        /// <param name="entity">Entity to insert</param>
        /// <returns>The inserted entity with any server-generated values</returns>
        Task<T?> CreateAsync(T entity);

        /// <summary>
        /// Inserts multiple entities in a single operation
        /// </summary>
        /// <param name="entities">Collection of entities to insert</param>
        /// <returns>The inserted entities with any server-generated values</returns>
        Task<List<T>> CreateManyAsync(List<T> entities);

        /// <summary>
        /// Updates an existing entity
        /// </summary>
        /// <param name="entity">Entity to update</param>
        /// <param name="id">Primary key value of the entity to update</param>
        /// <returns>The updated entity</returns>
        Task<T?> UpdateAsync(T entity, object id);

        /// <summary>
        /// Inserts an entity if it doesn't exist, or updates it if it does (upsert)
        /// </summary>
        /// <param name="entity">Entity to insert or update</param>
        /// <param name="column">The column name to use for conflict resolution</param>
        /// <returns>The inserted or updated entity</returns>
        Task<T?> UpsertAsync(T entity, string column);

        /// <summary>
        /// Inserts multiple entities if they don't exist, or updates them if they do (upsert)
        /// </summary>
        /// <param name="entities">Entities to insert or update</param>
        /// <param name="column">The column name to use for conflict resolution</param>
        /// <returns>The inserted or updated entities</returns>
        Task<List<T>> UpsertManyAsync(List<T> entities, string column);

        /// <summary>
        /// Deletes an entity by its primary key
        /// </summary>
        /// <param name="id">The primary key value</param>
        /// <returns>True if the entity was deleted, false otherwise</returns>
        Task<bool> DeleteAsync(object id);
    }
}
