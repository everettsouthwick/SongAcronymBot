using Microsoft.EntityFrameworkCore;
using SongAcronymBot.Domain.Data;

namespace SongAcronymBot.Domain.Repositories
{
    public interface IRepository<TEntity>
    {
        IQueryable<TEntity> GetAll();
        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    }

    public class Repository<TEntity>(SongAcronymBotContext context) : IRepository<TEntity> where TEntity : class, new()
    {
        protected readonly SongAcronymBotContext _context = context;
        private readonly AsyncLock _asyncLock = new();

        public IQueryable<TEntity> GetAll()
        {
            try
            {
                return _context.Set<TEntity>().AsNoTracking();
            }
            catch (Exception ex)
            {
                throw new Exception($"Couldn't retrieve entities: {ex.Message}");
            }
        }

        public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                using (await _asyncLock.LockAsync(cancellationToken))
                {
                    await _context.AddAsync(entity, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                    return entity;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new Exception($"{nameof(entity)} could not be saved: {ex.Message}");
            }
        }

        public async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entities);

            try
            {
                using (await _asyncLock.LockAsync(cancellationToken))
                {
                    await _context.AddRangeAsync(entities, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                    return entities;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new Exception($"{nameof(entities)} could not be saved: {ex.Message}");
            }
        }

        public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                using (await _asyncLock.LockAsync(cancellationToken))
                {
                    _context.Update(entity);
                    await _context.SaveChangesAsync(cancellationToken);
                    return entity;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new Exception($"{nameof(entity)} could not be updated: {ex.Message}");
            }
        }
    }

    public sealed class AsyncLock
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly Task<IDisposable> _releaser;

        public AsyncLock()
        {
            _releaser = Task.FromResult((IDisposable)new Releaser(this));
        }

        public async Task<IDisposable> LockAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            return await _releaser;
        }

        private sealed class Releaser : IDisposable
        {
            private readonly AsyncLock _toRelease;
            internal Releaser(AsyncLock toRelease) { _toRelease = toRelease; }
            public void Dispose() => _toRelease._semaphore.Release();
        }
    }
}
