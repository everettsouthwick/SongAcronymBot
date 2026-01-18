using SongAcronymBot.Domain.Supabase.Services;
using Supabase.Interfaces;
using Supabase.Postgrest.Models;
using Supabase.Realtime;

namespace SongAcronymBot.Domain.Supabase.Repositories
{
    /// <summary>
    /// Generic repository implementation for CRUD operations on Supabase tables
    /// </summary>
    /// <typeparam name="T">The entity type that inherits from BaseModel</typeparam>
    public class BaseRepository<T>(ISupabaseService supabaseService) : IBaseRepository<T> where T : BaseModel, new()
    {
        protected readonly ISupabaseService _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));

        /// <summary>
        /// Gets a query builder for the current model type
        /// </summary>
        /// <remarks>
        /// The schema is configured globally in SupabaseOptions and will be used automatically.
        /// </remarks>
        protected virtual ISupabaseTable<T, RealtimeChannel> GetQueryBuilder()
        {
            return _supabaseService.GetClient().From<T>();
        }

        /// <inheritdoc/>
        public virtual async Task<List<T>> GetAllAsync()
        {
            var response = await GetQueryBuilder().Get();
            return response.Models;
        }

        /// <inheritdoc/>
        public virtual async Task<T?> GetByIdAsync(object id)
        {
            var response = await GetQueryBuilder()
                .Filter("id", global::Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Single();

            return response;
        }

        /// <inheritdoc/>
        public virtual async Task<T?> CreateAsync(T entity)
        {
            var response = await GetQueryBuilder()
                .Insert(entity);

            return response.Models.FirstOrDefault();
        }

        /// <inheritdoc/>
        public virtual async Task<List<T>> CreateManyAsync(List<T> entities)
        {
            var response = await GetQueryBuilder()
                .Insert(entities);

            return response.Models;
        }

        /// <inheritdoc/>
        public virtual async Task<T?> UpdateAsync(T entity, object id)
        {
            var response = await GetQueryBuilder()
                .Filter("id", global::Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Update(entity);

            return response.Models.FirstOrDefault();
        }

        /// <inheritdoc/>
        public virtual async Task<T?> UpsertAsync(T entity, string column)
        {
            var response = await GetQueryBuilder()
                .OnConflict(column)
                .Upsert(entity);

            return response.Models.FirstOrDefault();
        }

        /// <inheritdoc/>
        public virtual async Task<List<T>> UpsertManyAsync(List<T> entities, string column)
        {
            var response = await GetQueryBuilder()
                .OnConflict(column)
                .Upsert(entities);

            return response.Models;
        }

        /// <inheritdoc/>
        public virtual async Task<bool> DeleteAsync(object id)
        {
            await GetQueryBuilder()
                .Filter("id", global::Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                .Delete();

            return true;
        }
    }
}
