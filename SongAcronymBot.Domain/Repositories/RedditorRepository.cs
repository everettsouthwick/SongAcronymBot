using Microsoft.EntityFrameworkCore;
using SongAcronymBot.Domain.Data;
using SongAcronymBot.Domain.Models;

namespace SongAcronymBot.Domain.Repositories
{
    public interface IRedditorRepository : IRepository<Redditor>
    {
        Task<Redditor?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<Redditor?> GetByNameAsync(string username, CancellationToken cancellationToken = default);
        Task<List<Redditor>> GetAllDisabled(CancellationToken cancellationToken = default);
    }

    public class RedditorRepository : Repository<Redditor>, IRedditorRepository
    {
        private new readonly SongAcronymBotContext _context;
        private readonly AsyncLock _asyncLock;

        public RedditorRepository(SongAcronymBotContext context) : base(context)
        {
            _context = context;
            _asyncLock = new AsyncLock();
        }

        public async Task<Redditor?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                using (await _asyncLock.LockAsync(cancellationToken))
                {
                    return await _context.Set<Redditor>()
                        .AsNoTracking()
                        .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new Exception($"Couldn't retrieve entity: {ex.Message}");
            }
        }

        public async Task<Redditor?> GetByNameAsync(string username, CancellationToken cancellationToken = default)
        {
            try
            {
                using (await _asyncLock.LockAsync(cancellationToken))
                {
                    return await _context.Set<Redditor>()
                        .AsNoTracking()
                        .SingleOrDefaultAsync(x => x.Username == username, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new Exception($"Couldn't retrieve entity: {ex.Message}");
            }
        }

        public async Task<List<Redditor>> GetAllDisabled(CancellationToken cancellationToken = default)
        {
            try
            {
                using (await _asyncLock.LockAsync(cancellationToken))
                {
                    return await _context.Set<Redditor>()
                        .AsNoTracking()
                        .Where(x => !x.Enabled)
                        .ToListAsync(cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new Exception($"Couldn't retrieve entities: {ex.Message}");
            }
        }
    }
}
